#include "pch.h"

void MyProc2(unsigned char* pixels, double* gamma, int pixelsLength)
{
    double gammaL =  *gamma;
    gammaL = 1 / gammaL;
    double L255 = 255.0;
    double One = 1.0;

    for (int i = 0; i < pixelsLength; i++)
    {
        double pixel = pixels[i] / L255;
        //(1-x)^2/2    ;log(1-x)= -x -x^2/2 -x^3/3 dla x bliskich 0
        pixel = One - pixel;

        double pixel2 = pixel * pixel;
        double pixel3 = pixel2 * pixel;

        pixel2 /= (One + One);
        pixel3 /= (One + One + One);

        pixel *= (-One);
        pixel = pixel - pixel2 - pixel3;
        pixel = pixel * gammaL;

        pixel2 = pixel * pixel;
        pixel3 = pixel2 * pixel;

        pixel += One;

        pixel2 /= (One * 2);
        pixel3 /= (One * 6);

        pixel = pixel + pixel2 + pixel3;
        pixel *= L255;

        if (pixel > L255) {
            pixel = L255;
        }
        else if (pixel < One) {
            pixel = One;
        }

        pixels[i] = (unsigned char)pixel;

    }
}
