#include "HubLink.h"

#pragma comment(lib, "ws2_32.lib")

namespace lattice {

int HubLink::s_refs = 0;

HubLink::HubLink() : m_sock(INVALID_SOCKET) {
    if (s_refs++ == 0) {
        WSADATA d;
        WSAStartup(MAKEWORD(2, 2), &d);
    }
}

HubLink::~HubLink() {
    hangup();
    if (--s_refs == 0) WSACleanup();
}

bool HubLink::dial(const std::string& host, uint16_t port) {
    hangup();

    addrinfo hints{};
    hints.ai_family = AF_INET;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_protocol = IPPROTO_TCP;

    addrinfo* res = nullptr;
    if (getaddrinfo(host.c_str(), std::to_string(port).c_str(), &hints, &res) != 0) {
        return false;
    }

    for (addrinfo* ai = res; ai != nullptr; ai = ai->ai_next) {
        SOCKET s = socket(ai->ai_family, ai->ai_socktype, ai->ai_protocol);
        if (s == INVALID_SOCKET) continue;
        if (connect(s, ai->ai_addr, static_cast<int>(ai->ai_addrlen)) == 0) {
            m_sock = s;
            break;
        }
        closesocket(s);
    }
    freeaddrinfo(res);

    if (m_sock != INVALID_SOCKET) {
        int nodelay = 1;
        setsockopt(m_sock, IPPROTO_TCP, TCP_NODELAY,
                   reinterpret_cast<char*>(&nodelay), sizeof(nodelay));
    }
    return m_sock != INVALID_SOCKET;
}

void HubLink::hangup() {
    std::lock_guard<std::mutex> lk(m_io);
    if (m_sock != INVALID_SOCKET) {
        shutdown(m_sock, SD_BOTH);
        closesocket(m_sock);
        m_sock = INVALID_SOCKET;
    }
}

bool HubLink::send(const uint8_t* data, size_t n) {
    std::lock_guard<std::mutex> lk(m_io);
    if (m_sock == INVALID_SOCKET) return false;
    size_t off = 0;
    while (off < n) {
        const int w = ::send(m_sock, reinterpret_cast<const char*>(data + off),
                             static_cast<int>(n - off), 0);
        if (w <= 0) return false;
        off += w;
    }
    return true;
}

bool HubLink::send(uint8_t b) { return send(&b, 1); }

bool HubLink::recv(uint8_t* data, size_t n) {
    std::lock_guard<std::mutex> lk(m_io);
    if (m_sock == INVALID_SOCKET) return false;
    size_t off = 0;
    while (off < n) {
        const int r = ::recv(m_sock, reinterpret_cast<char*>(data + off),
                             static_cast<int>(n - off), 0);
        if (r <= 0) return false;
        off += r;
    }
    return true;
}

}
