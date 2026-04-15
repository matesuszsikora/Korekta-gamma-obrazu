# Korekta-gamma-obrazu


Projekt realizuje korekcję gamma na plikach .bmp z wykorzystaniem technologii wielowątkowości i dynamicznie linkowanych bibliotek (DLL). Korekcja gamma pozwala na zmianę jasności obrazu w sposób nieliniowy, uwzględniając percepcję ludzkiego oka. Program umożliwia wybór między dwoma implementacjami korekcji gamma:
- Wykonanej w języku C.
- Wykonanej w języku asemblera (ASM) z użyciem zestawu instrukcji SIMD AVX2.
Aplikacja pozwala na:
- Ładowanie obrazu wejściowego.
- Przetwarzanie obrazu za pomocą korekcji gamma.
- Generowanie pliku z danymi do stworzenia histogramu porównawczego w formacie CSV.
