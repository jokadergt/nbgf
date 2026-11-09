#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>
#include <TlHelp32.h>
#include <algorithm>
#include <cstring>
#include <mutex>
#include <string>
#include <vector>
#include "memory/session_profile_reader.h"
#define API extern "C" __declspec(dllexport)
namespace {
std::mutex gate;
HANDLE process=nullptr;
DWORD processId=0;
uintptr_t base=0,model=0;
std::string error;
bool Read(uintptr_t address,void* data,size_t length) {
    SIZE_T actual=0;
    return process && ReadProcessMemory(process,reinterpret_cast<const void*>(address),data,length,&actual) && actual==length;
}
uintptr_t Base(DWORD pid) {
    HANDLE snapshot=CreateToolhelp32Snapshot(TH32CS_SNAPMODULE|TH32CS_SNAPMODULE32,pid);
    if(snapshot==INVALID_HANDLE_VALUE) return 0;
    MODULEENTRY32W entry{};entry.dwSize=sizeof(entry);
    uintptr_t result=Module32FirstW(snapshot,&entry)?reinterpret_cast<uintptr_t>(entry.modBaseAddr):0;
    CloseHandle(snapshot);return result;
}
void Reset() { if(process) CloseHandle(process);process=nullptr;processId=0;base=0;model=0; }
uintptr_t Locate() { cesi::SessionProfileReader<decltype(&Read)> reader(&Read);return model=reader.FindActive(base); }
}
API bool __stdcall Initialize(){return true;}
API const char* __stdcall GetCoreOffsetVersion(){return "version-bc1bf755618240e8";}
API const char* __stdcall GetCoreExeSha256(){return "57E5C4203D420815D7704FA53FA1A63E09D1190E4A55439356B41B1101AF2120";}
API bool __stdcall IsExecutionReady(){return false;}
API void __stdcall RedirConsole(){}
API DWORD __stdcall FindRobloxProcess(){
    HANDLE snapshot=CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS,0);
    if(snapshot==INVALID_HANDLE_VALUE)return 0;
    PROCESSENTRY32W entry{};entry.dwSize=sizeof(entry);DWORD result=0;
    if(Process32FirstW(snapshot,&entry))do{if(lstrcmpiW(entry.szExeFile,L"RobloxPlayerBeta.exe")==0){result=entry.th32ProcessID;break;}}while(Process32NextW(snapshot,&entry));
    CloseHandle(snapshot);return result;
}
API bool __stdcall Connect(DWORD pid){
    std::lock_guard<std::mutex> lock(gate);Reset();error.clear();
    process=OpenProcess(PROCESS_QUERY_INFORMATION|PROCESS_VM_READ,FALSE,pid);
    if(!process){error="OpenProcess failed: "+std::to_string(GetLastError());return false;}
    processId=pid;base=Base(pid);
    if(!base || !Locate()) {error="Active DataModel not found. Enter an experience, wait for your character, and retry. This core targets version-bc1bf755618240e8.";Reset();return false;}
    return true;
}
API void __stdcall Disconnect(){std::lock_guard<std::mutex> lock(gate);Reset();}
API DWORD __stdcall GetRobloxPid(){std::lock_guard<std::mutex> lock(gate);return processId;}
API uintptr_t __stdcall GetDataModel(){std::lock_guard<std::mutex> lock(gate);return Locate();}
API bool __stdcall ReadMemory(uintptr_t address,void* buffer,size_t size){std::lock_guard<std::mutex> lock(gate);return buffer&&Read(address,buffer,size);}
API bool __stdcall WriteMemory(uintptr_t,const void*,size_t){return false;}
API int __stdcall ExecuteScript(const char*,int){std::lock_guard<std::mutex> lock(gate);error="Script execution pending: bytecode encoding and capabilities are not validated for this profile. No script executed.";return -9;}
API int __stdcall GetLastExecError(char* buffer,int size){
    std::lock_guard<std::mutex> lock(gate);if(!buffer||size<=0)return 0;
    int count=static_cast<int>(std::min(error.size(),size_t(size-1)));std::memcpy(buffer,error.data(),count);buffer[count]=0;return count;
}
API int __stdcall GetJobCount(){
    std::lock_guard<std::mutex> lock(gate);cesi::SessionProfileReader<decltype(&Read)> reader(&Read);
    auto scheduler=reader.Pointer(base,cesi::profile::RVA::TaskSchedulerGlobal);
    auto start=reader.Pointer(scheduler,cesi::profile::TaskScheduler::JobStart),end=reader.Pointer(scheduler,cesi::profile::TaskScheduler::JobEnd);
    return start&&end>=start&&(end-start)%16==0&&(end-start)/16<=4096?static_cast<int>((end-start)/16):-1;
}
API bool __stdcall GetClientInfo(char* buffer,int size){
    std::lock_guard<std::mutex> lock(gate);if(!buffer||size<=0||!Locate())return false;
    cesi::SessionProfileReader<decltype(&Read)> reader(&Read);
    auto players=reader.ChildOfClass(model,"Players"),local=reader.Pointer(players,cesi::profile::Players::LocalPlayer);
    auto name=reader.Name(local);uint64_t user=0;
    if(name.empty()||!Read(local+cesi::profile::Player::UserId,&user,sizeof(user))||!user||user>9007199254740991ULL)return false;
    std::string value=name+"|"+std::to_string(user)+"|structural-test|0";
    if(value.size()>=size_t(size))return false;
    std::memcpy(buffer,value.c_str(),value.size()+1);return true;
}
