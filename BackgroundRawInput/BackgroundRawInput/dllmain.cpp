#include "pch.h"
#include <windows.h>
#include <thread>
#include <atomic>
#include <chrono>
#include <cstdint>

#pragma comment(lib, "user32.lib")

// ============================
// ENUM
// ============================

enum class RawKey : uint16_t
{
    Unknown = 0
};

enum class KeyEventType : uint8_t
{
    Down = 0,
    Up = 1
};

typedef void(__stdcall* KeyCallback)(
    RawKey key,
    KeyEventType type,
    uint64_t timestamp);

// ============================
// Context
// ============================

struct RawInputContext
{
    std::thread worker;
    DWORD threadId = 0;
    HWND hwnd = nullptr;

    std::atomic<bool> running{ false };
    std::atomic<bool> stopping{ false };

    KeyCallback callback = nullptr;
};

// 전역 컨텍스트 추적 (안전 종료용)
static std::atomic<RawInputContext*> g_ctx{ nullptr };

// ============================
// 유틸
// ============================

uint64_t GetUnixMillis()
{
    using namespace std::chrono;
    return duration_cast<std::chrono::milliseconds>(
        std::chrono::system_clock::now().time_since_epoch()
    ).count();
}

// ============================
// WindowProc
// ============================

LRESULT CALLBACK WindowProc(
    HWND hwnd,
    UINT msg,
    WPARAM wParam,
    LPARAM lParam)
{
    if (msg == WM_INPUT)
    {
        UINT size = 0;
        GetRawInputData((HRAWINPUT)lParam,
            RID_INPUT,
            nullptr,
            &size,
            sizeof(RAWINPUTHEADER));

        BYTE* buffer = new BYTE[size];

        if (GetRawInputData((HRAWINPUT)lParam,
            RID_INPUT,
            buffer,
            &size,
            sizeof(RAWINPUTHEADER)) == size)
        {
            RAWINPUT* raw = (RAWINPUT*)buffer;

            if (raw->header.dwType == RIM_TYPEKEYBOARD)
            {
                auto* ctx = (RawInputContext*)
                    GetWindowLongPtr(hwnd, GWLP_USERDATA);

                if (ctx && ctx->callback && ctx->running)
                {
                    USHORT vk = raw->data.keyboard.VKey;
                    bool isDown =
                        !(raw->data.keyboard.Flags & RI_KEY_BREAK);

                    ctx->callback(
                        (RawKey)vk,
                        isDown ? KeyEventType::Down
                        : KeyEventType::Up,
                        GetUnixMillis());
                }
            }
        }

        delete[] buffer;
    }

    return DefWindowProc(hwnd, msg, wParam, lParam);
}

// ============================
// ThreadProc
// ============================

void ThreadProc(RawInputContext* ctx)
{
    ctx->threadId = GetCurrentThreadId();

    HINSTANCE hInstance = GetModuleHandle(nullptr);

    WNDCLASS wc = {};
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = L"BackgroundRawInputWindow";

    RegisterClass(&wc);

    ctx->hwnd = CreateWindowEx(
        0,
        wc.lpszClassName,
        L"",
        0,
        0, 0, 0, 0,
        HWND_MESSAGE,
        nullptr,
        hInstance,
        nullptr);

    SetWindowLongPtr(ctx->hwnd,
        GWLP_USERDATA,
        (LONG_PTR)ctx);

    RAWINPUTDEVICE rid = {};
    rid.usUsagePage = 0x01;
    rid.usUsage = 0x06;
    rid.dwFlags = RIDEV_INPUTSINK;
    rid.hwndTarget = ctx->hwnd;

    RegisterRawInputDevices(&rid, 1, sizeof(rid));

    ctx->running = true;

    MSG msg;
    while (GetMessage(&msg, nullptr, 0, 0))
    {
        if (ctx->stopping)
            break;

        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    ctx->running = false;

    if (ctx->hwnd)
        DestroyWindow(ctx->hwnd);
}

// ============================
// 안전 종료 함수
// ============================

void StopContext(RawInputContext* ctx)
{
    if (!ctx)
        return;

    if (ctx->stopping.exchange(true))
        return; // 이미 종료 중

    if (ctx->threadId != 0)
    {
        PostThreadMessage(ctx->threadId,
            WM_QUIT, 0, 0);
    }

    // join 타임아웃 방어
    if (ctx->worker.joinable())
    {
        auto start = std::chrono::steady_clock::now();

        while (ctx->running)
        {
            std::this_thread::sleep_for(
                std::chrono::milliseconds(10));

            auto now = std::chrono::steady_clock::now();
            if (std::chrono::duration_cast<std::chrono::seconds>(
                now - start).count() > 2)
            {
                break; // 2초 타임아웃
            }
        }

        ctx->worker.join();
    }

    delete ctx;
    g_ctx = nullptr;
}

// ============================
// 외부 API
// ============================

extern "C"
__declspec(dllexport)
uint64_t __stdcall _InitializeRawInput(
    KeyCallback callback)
{
    auto* ctx = new RawInputContext();
    ctx->callback = callback;

    g_ctx = ctx;

    ctx->worker = std::thread(ThreadProc, ctx);

    return (uint64_t)ctx;
}

extern "C"
__declspec(dllexport)
void __stdcall _StopRawInput(uint64_t handle)
{
    StopContext((RawInputContext*)handle);
}

// ============================
// DLL 종료 안전장치
// ============================

BOOL WINAPI DllMain(
    HINSTANCE hinstDLL,
    DWORD fdwReason,
    LPVOID lpReserved)
{
    if (fdwReason == DLL_PROCESS_DETACH)
    {
        RawInputContext* ctx = g_ctx.load();
        if (ctx)
        {
            StopContext(ctx);
        }
    }

    return TRUE;
}