#pragma once

#include <winsock2.h>
#include <ws2tcpip.h>

#include <cstdint>
#include <mutex>
#include <string>
#include <vector>

namespace lattice {

class HubLink {
public:
    HubLink();
    ~HubLink();

    bool dial(const std::string& host, uint16_t port);
    void hangup();
    bool isOpen() const { return m_sock != INVALID_SOCKET; }

    bool send(const uint8_t* data, size_t n);
    bool send(uint8_t b);
    bool recv(uint8_t* data, size_t n);

    template <typename T>
    bool sendPod(const T& v) {
        return send(reinterpret_cast<const uint8_t*>(&v), sizeof(T));
    }

    template <typename T>
    bool recvPod(T& v) {
        return recv(reinterpret_cast<uint8_t*>(&v), sizeof(T));
    }

private:
    SOCKET m_sock;
    mutable std::mutex m_io;
    static int s_refs;
};

}
