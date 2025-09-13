#include <windows.h>

#include <string>

#include "CaptureApp.h"

int APIENTRY wWinMain(_In_ HINSTANCE, _In_opt_ HINSTANCE, _In_ LPWSTR cmdLine, _In_ int) {
    std::wstring args(cmdLine ? cmdLine : L"");
    std::string host = "127.0.0.1";
    if (!args.empty()) {
        host.assign(args.begin(), args.end());
    }
    lattice::CaptureApp app;
    return app.run(host);
}
