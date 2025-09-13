#pragma once

#include <cstdint>

namespace lattice {

struct Pixel {
    uint8_t b;
    uint8_t g;
    uint8_t r;
    uint8_t a;

    Pixel() : b(0), g(0), r(0), a(255) {}
    Pixel(uint8_t rv, uint8_t gv, uint8_t bv) : b(bv), g(gv), r(rv), a(255) {}
};

}
