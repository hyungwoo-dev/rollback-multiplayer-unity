#include "pch.h"
#include <windows.h>
#include <thread>
#include <atomic>
#include <vector>
#include <mutex>
#include <chrono>
#include <cstdint>
#include <algorithm>

#pragma comment(lib, "user32.lib")

// =======================================================
// ENUM
// =======================================================

enum class RawKey : uint16_t
{
	Unknown = 0,

	LeftArrow = VK_LEFT,
	UpArrow = VK_UP,
	RightArrow = VK_RIGHT,
	DownArrow = VK_DOWN,
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

// =======================================================
// Subscriber
// =======================================================

struct Subscriber
{
	uint64_t id;
	KeyCallback callback;
};

// =======================================================
// Context (프로세스당 단 하나)
// =======================================================

struct RawInputContext
{
	std::thread worker;
	DWORD threadId = 0;
	HWND hwnd = nullptr;

	std::atomic<bool> running{ false };
	std::atomic<bool> stopping{ false };

	std::vector<Subscriber> subscribers;
	std::mutex subMutex;

	std::atomic<uint64_t> nextId{ 1 };
};

static RawInputContext g_ctx;
static std::mutex g_initMutex;

// =======================================================
// Time
// =======================================================

uint64_t GetUnixMillis()
{
	using namespace std::chrono;
	return duration_cast<std::chrono::milliseconds>(
		std::chrono::system_clock::now().time_since_epoch()
	).count();
}

// =======================================================
// WindowProc
// =======================================================

LRESULT CALLBACK WindowProc(
	HWND hwnd,
	UINT msg,
	WPARAM wParam,
	LPARAM lParam)
{
	if (msg == WM_INPUT)
	{
		UINT size = 0;

		GetRawInputData(
			(HRAWINPUT)lParam,
			RID_INPUT,
			nullptr,
			&size,
			sizeof(RAWINPUTHEADER));

		if (size > 0)
		{
			std::vector<BYTE> buffer(size);

			if (GetRawInputData(
				(HRAWINPUT)lParam,
				RID_INPUT,
				buffer.data(),
				&size,
				sizeof(RAWINPUTHEADER)) == size)
			{
				RAWINPUT* raw =
					reinterpret_cast<RAWINPUT*>(buffer.data());

				if (raw->header.dwType == RIM_TYPEKEYBOARD)
				{
					USHORT vk = raw->data.keyboard.VKey;

					bool isDown =
						!(raw->data.keyboard.Flags & RI_KEY_BREAK);

					uint64_t timestamp = GetUnixMillis();

					std::lock_guard<std::mutex> lock(g_ctx.subMutex);

					for (auto& sub : g_ctx.subscribers)
					{
						if (sub.callback)
						{
							sub.callback(
								static_cast<RawKey>(vk),
								isDown ? KeyEventType::Down
								: KeyEventType::Up,
								timestamp);
						}
					}
				}
			}
		}
	}

	return DefWindowProc(hwnd, msg, wParam, lParam);
}

// =======================================================
// ThreadProc
// =======================================================

void ThreadProc()
{
	g_ctx.threadId = GetCurrentThreadId();

	HINSTANCE hInstance = GetModuleHandle(nullptr);

	WNDCLASS wc = {};
	wc.lpfnWndProc = WindowProc;
	wc.hInstance = hInstance;
	wc.lpszClassName = L"BackgroundRawInputWindow";

	if (!RegisterClass(&wc))
	{
		if (GetLastError() != ERROR_CLASS_ALREADY_EXISTS)
			return;
	}

	g_ctx.hwnd = CreateWindowEx(
		0,
		wc.lpszClassName,
		L"",
		WS_OVERLAPPED,
		0, 0, 1, 1,
		nullptr,
		nullptr,
		hInstance,
		nullptr);

	if (!g_ctx.hwnd)
		return;

	ShowWindow(g_ctx.hwnd, SW_HIDE);

	RAWINPUTDEVICE rid = {};
	rid.usUsagePage = 0x01;  // Generic Desktop Controls
	rid.usUsage = 0x06;      // Keyboard
	rid.dwFlags = RIDEV_EXINPUTSINK; // 🔥 멀티 프로세스 안정
	rid.hwndTarget = g_ctx.hwnd;

	if (!RegisterRawInputDevices(&rid, 1, sizeof(rid)))
		return;

	g_ctx.running = true;

	MSG msg;
	while (GetMessage(&msg, nullptr, 0, 0))
	{
		if (g_ctx.stopping)
			break;

		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}

	g_ctx.running = false;

	if (g_ctx.hwnd)
	{
		DestroyWindow(g_ctx.hwnd);
		g_ctx.hwnd = nullptr;
	}
}

// =======================================================
// Initialize (구독 추가)
// =======================================================

extern "C"
__declspec(dllexport)
uint64_t __stdcall _InitializeRawInput(
	KeyCallback callback)
{
	std::lock_guard<std::mutex> lock(g_initMutex);

	if (!g_ctx.running)
	{
		g_ctx.stopping = false;
		g_ctx.worker = std::thread(ThreadProc);
	}

	uint64_t id = g_ctx.nextId++;

	{
		std::lock_guard<std::mutex> subLock(g_ctx.subMutex);
		g_ctx.subscribers.push_back({ id, callback });
	}

	return id;
}

// =======================================================
// Stop (구독 제거)
// =======================================================

extern "C"
__declspec(dllexport)
void __stdcall _StopRawInput(uint64_t id)
{
	std::lock_guard<std::mutex> lock(g_initMutex);

	{
		std::lock_guard<std::mutex> subLock(g_ctx.subMutex);

		g_ctx.subscribers.erase(
			std::remove_if(
				g_ctx.subscribers.begin(),
				g_ctx.subscribers.end(),
				[id](const Subscriber& s)
				{
					return s.id == id;
				}),
			g_ctx.subscribers.end());
	}

	if (g_ctx.subscribers.empty() && g_ctx.running)
	{
		g_ctx.stopping = true;

		PostThreadMessage(
			g_ctx.threadId,
			WM_QUIT,
			0,
			0);

		if (g_ctx.worker.joinable())
			g_ctx.worker.join();
	}
}

// =======================================================
// DLL 안전 종료
// =======================================================

BOOL WINAPI DllMain(
	HINSTANCE hinstDLL,
	DWORD fdwReason,
	LPVOID lpReserved)
{
	if (fdwReason == DLL_PROCESS_DETACH)
	{
		if (g_ctx.running)
		{
			g_ctx.stopping = true;

			PostThreadMessage(
				g_ctx.threadId,
				WM_QUIT,
				0,
				0);

			if (g_ctx.worker.joinable())
				g_ctx.worker.join();
		}
	}

	return TRUE;
}