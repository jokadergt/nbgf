#pragma once
#include <cstdint>
#include <cstring>
#include <map>
#include <string>
#include <vector>
#include "profiles/bc1bf755618240e8-session.h"
namespace cesi {
namespace profile = roblox_bc1bf755618240e8;
template<class Read> class SessionProfileReader {
    Read read;
    bool Range(uintptr_t a, size_t n = 1) const { return a >= 0x10000 && a <= 0x7FFFFFFFFFFFULL && n <= 0x7FFFFFFFFFFFULL - a; }
public:
    explicit SessionProfileReader(Read reader) : read(reader) {}
    uintptr_t Pointer(uintptr_t a, uintptr_t offset = 0) {
        uintptr_t result = 0;
        if (!Range(a) || offset > 0x7FFFFFFFFFFFULL-a || !Range(a+offset,sizeof(result))) return 0;
        return read(a+offset,&result,sizeof(result)) && Range(result) ? result : 0;
    }
    std::string String(uintptr_t a) {
        struct Value { union { char inlineData[16]; uintptr_t pointer; }; uint64_t size,capacity; } value{};
        if (!Range(a,sizeof(value)) || !read(a,&value,sizeof(value)) || value.size==0 || value.size>512 || value.capacity<value.size) return {};
        if (value.capacity<=15) return std::string(value.inlineData,size_t(value.size));
        if (!Range(value.pointer,size_t(value.size))) return {};
        std::string result(size_t(value.size),'\0');
        return read(value.pointer,result.data(),result.size()) ? result : std::string{};
    }
    std::string Name(uintptr_t object) { return String(Pointer(object,profile::Instance::Name)); }
    std::string Class(uintptr_t object) { return String(Pointer(Pointer(object,profile::Instance::ClassDescriptor),profile::ClassDescriptor::Name)); }
    std::vector<uintptr_t> Children(uintptr_t object) {
        auto container=Pointer(object,profile::Instance::Children);
        auto begin=Pointer(container,profile::ChildrenVector::Begin),end=Pointer(container,profile::ChildrenVector::End);
        if (!begin || !end || end<begin || (end-begin)%profile::ChildrenVector::Stride || end-begin>profile::ChildrenVector::Stride*4096) return {};
        std::vector<uintptr_t> result;
        for (auto at=begin;at<end;at+=profile::ChildrenVector::Stride) {
            auto child=Pointer(at);
            if (child && Pointer(child,profile::Instance::Parent)==object) result.push_back(child);
        }
        return result;
    }
    uintptr_t ChildOfClass(uintptr_t object,const std::string& type) {
        for(auto child:Children(object)) if(Class(child)==type) return child;
        return 0;
    }
    bool IsActive(uintptr_t object) {
        if(Class(object)!="DataModel") return false;
        uintptr_t parent=1;
        if(!read(object+profile::Instance::Parent,&parent,sizeof(parent)) || parent) return false;
        auto workspace=Pointer(object,profile::DataModel::Workspace);
        if(Class(workspace)!="Workspace" || Pointer(workspace,profile::Instance::Parent)!=object) return false;
        auto players=ChildOfClass(object,"Players"),local=Pointer(players,profile::Players::LocalPlayer);
        return Class(local)=="Player" && Pointer(local,profile::Instance::Parent)==players && !Name(local).empty();
    }
    uintptr_t FindActive(uintptr_t base) {
        auto adjusted=[&](uintptr_t primary) { return Range(primary,profile::PointerAdjustment::PrimaryToInstance)
            ? primary+profile::PointerAdjustment::PrimaryToInstance : uintptr_t(0); };
        uintptr_t candidates[]={adjusted(Pointer(base,profile::RVA::ActiveDataModelPrimaryA)),
            adjusted(Pointer(base,profile::RVA::ActiveDataModelPrimaryB)),
            adjusted(Pointer(Pointer(base,profile::RVA::VisualEngineGlobal),profile::VisualEngine::PrimaryDataModel))};
        std::map<uintptr_t,int> votes;
        for(auto value:candidates) if(value) ++votes[value];
        for(auto [candidate,count]:votes) if(count>=2 && IsActive(candidate)) return candidate;
        return 0;
    }
};
}
