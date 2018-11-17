
#include <windows.h>
#include <shellapi.h>
#include <stdio.h>

#define ID_TRAY_APP_ICON                5000
#define ID_TRAY_SHOW_CONTEXT_MENU_ITEM  4000
#define ID_TRAY_EXIT_CONTEXT_MENU_ITEM  3000
#define ID_TRAY_SEPARATOR_CONTEXT_MENU_ITEM  2000
#define WM_TRAYICON ( WM_USER + 1 )

UINT WM_TASKBARCREATED = 0;

HWND g_hwnd;
HMENU g_menu;

NOTIFYICONDATA g_notifyIconData;

LRESULT CALLBACK WndProc(HWND, UINT, WPARAM, LPARAM);


void ShowIcon()
{
	// add the icon to the system tray
	Shell_NotifyIcon(NIM_ADD, &g_notifyIconData);
}

void InitNotifyIconData()
{
	memset(&g_notifyIconData, 0, sizeof(NOTIFYICONDATA));

	g_notifyIconData.cbSize = sizeof(NOTIFYICONDATA);


	// The combination of HWND and uID for a unique identifier for each tray item.
	// Set the parent window handle for the notifyIcon
	g_notifyIconData.hWnd = g_hwnd;
	// Give notifyIcon an ID
	g_notifyIconData.uID = ID_TRAY_APP_ICON;

	// Set up flags.
	g_notifyIconData.uFlags = 
		NIF_ICON |		// An icon will be specified
		NIF_MESSAGE |	// Request window message to be sent to our WNDPROC
		NIF_TIP;		// A tooltip will be specified

	g_notifyIconData.uCallbackMessage = WM_TRAYICON; // This message must be handled in hwnd's window procedure. more info below.

	g_notifyIconData.hIcon = (HICON)LoadImage(NULL, TEXT("tray.ico"), IMAGE_ICON, 0, 0, LR_LOADFROMFILE);

	// Set the tooltip text.  must be LESS THAN 64 chars
	strcpy_s(g_notifyIconData.szTip, TEXT("Auto Mark Check Agent"));
}

//Entry point
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR args, int iCmdShow)
{
	TCHAR className[] = TEXT("AutoMarkCheckTrayCL");

	// Subscribe to TaskbarCreated windows event to be able to reshow the icon if the taskbar crashes.
	WM_TASKBARCREATED = RegisterWindowMessageA("TaskbarCreated");

	// Create parent dummy window for the icon.
	WNDCLASS wnd = { 0 };

	wnd.lpszClassName = className;
	wnd.lpfnWndProc = WndProc;

	//Register the window class
	if (!RegisterClass(&wnd))
	{
		FatalAppExit(0, TEXT("AutoMarkCheckTray: Couldn't register window class!"));
	}

	g_hwnd = CreateWindow(className, TEXT("Auto Mark Check Agent"), WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, NULL, NULL, hInstance, NULL);

	InitNotifyIconData();
	ShowIcon();

	MSG msg;
	while (GetMessage(&msg, NULL, 0, 0))
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}

	return msg.wParam;
}


LRESULT CALLBACK WndProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	if (message == WM_TASKBARCREATED) //When taskbar refreshes/crashes
	{
		ShowIcon();
		return 0;
	}

	switch (message)
	{
	case WM_CREATE:

		// create the menu once.
		g_menu = CreatePopupMenu();

		AppendMenu(g_menu, MF_STRING, ID_TRAY_SHOW_CONTEXT_MENU_ITEM, TEXT("Show"));
		AppendMenu(g_menu, MF_SEPARATOR | MF_BYPOSITION, ID_TRAY_SEPARATOR_CONTEXT_MENU_ITEM, NULL);
		AppendMenu(g_menu, MF_STRING, ID_TRAY_EXIT_CONTEXT_MENU_ITEM, TEXT("Exit"));

		break;


		// Our user defined WM_TRAYICON message.
		// We made this message up, and we told
		// 
	case WM_TRAYICON:
	{
		// the mouse button has been released.

		if (lParam == WM_LBUTTONDOWN) //Right click
		{
			printf("You have restored me!\n");
			//Launch GUI here
		}
		else if (lParam == WM_RBUTTONDOWN) //Left click
		{
			// Get current mouse position.
			POINT curPoint;
			GetCursorPos(&curPoint);

			// Set foreground window so menu shows up on top
			SetForegroundWindow(hwnd);



			// TrackPopupMenu blocks the app until TrackPopupMenu returns
			UINT clicked = TrackPopupMenu(

				g_menu,
				TPM_RETURNCMD | TPM_NONOTIFY, // don't send me WM_COMMAND messages about this window, instead return the identifier of the clicked menu item
				curPoint.x,
				curPoint.y,
				0,
				hwnd,
				NULL

			);

			if (clicked == ID_TRAY_EXIT_CONTEXT_MENU_ITEM)
			{
				PostQuitMessage(0);
			}
		}
	}
	break;

	case WM_DESTROY:
		PostQuitMessage(0);
		break;

	}

	return DefWindowProc(hwnd, message, wParam, lParam);
}
