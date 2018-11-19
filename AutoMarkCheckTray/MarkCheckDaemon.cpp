#include <chrono>
#include <thread>
#include <windows.h>
#include <process.h>
#include <Tlhelp32.h>

#include "MarkCheckDaemon.h"

#define RUN_INTERVAL 60	// 1 minute
#define MARK_CHECK_AGENT_EXE "AutoMarkCheckAgent.exe"

#pragma warning(disable:4996)

bool runDaemon = true;

using namespace std::chrono_literals;

void StartDaemon()
{
	std::thread([]()
	{
		while (runDaemon)
		{
			StartMarkCheck(false);
			std::this_thread::sleep_for(std::chrono::seconds(RUN_INTERVAL)); // Wait for interval to pass
		}
	}).detach();
}

void StartMarkCheck(bool gui)
{
	char * path = new char[MAX_PATH + 1];
	GetCurrentDirectory(MAX_PATH, path);
	std::string final(path);
	final += "\\" + std::string(MARK_CHECK_AGENT_EXE);

	STARTUPINFO si;
	PROCESS_INFORMATION pi;

	// set the size of the structures
	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	ZeroMemory(&pi, sizeof(pi));

	std::string cmdLine(MARK_CHECK_AGENT_EXE);

	if (gui)
		cmdLine += " -gui";

	LPSTR cmd = const_cast<LPSTR>(cmdLine.c_str());

	// start the program up
	CreateProcess(final.c_str(),   // the path
		cmd,			// Command line
		NULL,           // Process handle not inheritable
		NULL,           // Thread handle not inheritable
		FALSE,          // Set handle inheritance to FALSE
		0,              // No creation flags
		NULL,           // Use parent's environment block
		NULL,           // Use parent's starting directory 
		&si,            // Pointer to STARTUPINFO structure
		&pi             // Pointer to PROCESS_INFORMATION structure (removed extra parentheses)
	);
	// Close process and thread handles. 

	delete[] path;
	CloseHandle(pi.hProcess);
	CloseHandle(pi.hThread);
}

void StopMarkCheck()
{
	KillProcessByName(MARK_CHECK_AGENT_EXE);
}

void KillProcessByName(const char *filename)
{
	HANDLE hSnapShot = CreateToolhelp32Snapshot(TH32CS_SNAPALL, NULL);
	PROCESSENTRY32 pEntry;
	pEntry.dwSize = sizeof(pEntry);
	BOOL hRes = Process32First(hSnapShot, &pEntry);
	while (hRes)
	{
		if (strcmp(pEntry.szExeFile, filename) == 0)
		{
			HANDLE hProcess = OpenProcess(PROCESS_TERMINATE, 0,
				(DWORD)pEntry.th32ProcessID);
			if (hProcess != NULL)
			{
				TerminateProcess(hProcess, 9);
				CloseHandle(hProcess);
			}
		}
		hRes = Process32Next(hSnapShot, &pEntry);
	}
	CloseHandle(hSnapShot);
}

