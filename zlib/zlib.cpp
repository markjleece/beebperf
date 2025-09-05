#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "zlib/zlib.h"

//
// dll entry point
//
BOOL APIENTRY DllMain(HMODULE, DWORD, LPVOID)
{
    return TRUE;
}

//
// exported Inflate(...) api
//
extern "C" __declspec(dllexport) bool Inflate(
    unsigned char* compressedData, int compressedSize,
    unsigned char* uncompressedData, int uncompressedSize)
{
    z_stream inflateStream = { 0 };

    inflateStream.next_in = (Bytef*)compressedData;
    inflateStream.avail_in = compressedSize;
    inflateStream.next_out = uncompressedData;
    inflateStream.avail_out = uncompressedSize;

    if (inflateInit(&inflateStream) != Z_OK)
    {
        return false;
    }

    int ret = inflate(&inflateStream, Z_FINISH);
    if (ret != Z_STREAM_END) 
    {
        inflateEnd(&inflateStream);
        return false;
    }

    inflateEnd(&inflateStream);
    return true;
}
