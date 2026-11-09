#pragma once
#include <cstdint>
#include <cstddef>
#include <cstring>
#include <string>
#include <utility>

namespace cesi {
struct DataModelLayout {
    uintptr_t fakeGlobal, fakeToReal, visualGlobal, visualToFake;
    uintptr_t schedulerGlobal, jobsBegin, jobsEnd, renderJobToReal;
    uintptr_t classDescriptor, className;
};

// Reader is injected so discovery can be tested without a running client.
template<class Reader> class DataModelResolver {
    Reader read;
    DataModelLayout layout;
    static bool Address(uintptr_t value, size_t size = 1) {
        return value >= 0x10000 && value <= 0x7FFFFFFFFFFFULL
            && size <= 0x7FFFFFFFFFFFULL - value;
    }
    uintptr_t Pointer(uintptr_t base, uintptr_t offset = 0) {
        uintptr_t value = 0;
        if (!Address(base) || offset > 0x7FFFFFFFFFFFULL - base
            || !Address(base + offset, sizeof(value))) return 0;
        return read(base + offset, &value, sizeof(value)) && Address(value) ? value : 0;
    }
    std::string String(uintptr_t address) {
        struct S { union { char data[16]; uintptr_t pointer; }; uint64_t size, capacity; } value{};
        static_assert(sizeof(S) == 32);
        if (!Address(address, sizeof(value)) || !read(address, &value, sizeof(value))
            || value.size == 0 || value.size > 128 || value.capacity < value.size) return {};
        if (value.capacity < 16) return std::string(value.data, size_t(value.size));
        if (!Address(value.pointer, size_t(value.size))) return {};
        std::string result(size_t(value.size), '\0');
        return read(value.pointer, result.data(), result.size()) ? result : std::string{};
    }
public:
    DataModelResolver(Reader reader, DataModelLayout offsets) : read(std::move(reader)), layout(offsets) {}
    bool IsDataModel(uintptr_t candidate) {
        auto descriptor = Pointer(candidate, layout.classDescriptor);
        return descriptor && String(Pointer(descriptor, layout.className)) == "DataModel";
    }
    uintptr_t Find(uintptr_t base, uintptr_t cached = 0) {
        if (cached && IsDataModel(cached)) return cached;
        auto fake = Pointer(base, layout.fakeGlobal);
        auto candidate = Pointer(fake, layout.fakeToReal);
        if (IsDataModel(candidate)) return candidate;
        auto visual = Pointer(base, layout.visualGlobal);
        candidate = Pointer(Pointer(visual, layout.visualToFake), layout.fakeToReal);
        if (IsDataModel(candidate)) return candidate;
        auto scheduler = Pointer(base, layout.schedulerGlobal);
        auto begin = Pointer(scheduler, layout.jobsBegin), end = Pointer(scheduler, layout.jobsEnd);
        if (!begin || !end || end <= begin || (end - begin) % sizeof(uintptr_t)) return 0;
        size_t count = size_t((end - begin) / sizeof(uintptr_t));
        if (count > 4096) return 0;
        for (size_t i = 0; i < count; ++i) {
            auto job = Pointer(begin, i * sizeof(uintptr_t));
            candidate = Pointer(job, layout.renderJobToReal);
            if (IsDataModel(candidate)) return candidate;
        }
        return 0;
    }
};
}
