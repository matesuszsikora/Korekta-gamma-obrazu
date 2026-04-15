.code
MyProc1 PROC


    sub rsp, 32
    vmovups [rsp], ymm6

    push rbx
    push rbp
    push rdi
    push rsi
    push rsp
    
    pxor xmm3, xmm3
    movsd xmm3, qword ptr [rdx]

    mov rax, 1
    cvtsi2sd xmm6, rax
    divsd xmm6, xmm3                    ;1/gamma
    vbroadcastsd ymm6, xmm6

    cvtsi2sd xmm4, rax
    vbroadcastsd ymm4, xmm4             ;zapisaine 1.0 do ymm4

    mov rax, 255
    cvtsi2sd xmm5, rax
    vbroadcastsd ymm5, xmm5             ;zapisanie 255.0 do ymm5

LoopStart:

    cmp r8, 0
    jle EndLoop

    ; r
    movzx rax, byte ptr [rcx]          
    cvtsi2sd xmm0, rax                  

    ;b
    movzx rax, byte ptr [rcx + 1]      
    cvtsi2sd xmm1, rax                  

    ;g
    movzx rax, byte ptr [rcx + 2]      
    cvtsi2sd xmm2, rax              
    
    movzx rax, byte ptr [rcx + 3]
    cvtsi2sd xmm3, rax

    vunpcklpd xmm0, xmm0, xmm1          ; rb w xmm0
    vunpcklpd xmm2, xmm2, xmm3          ; 0g w xmm1

    vinsertf128 ymm0, ymm0, xmm0, 0     
    vinsertf128 ymm0, ymm0, xmm2, 1     
    ; r-g-b-r

    
    ;endload

    vdivpd ymm0, ymm0, ymm5 ; x = x/255

    vxorpd ymm3, ymm3, ymm3 ;ymm3 = 0

    ;log(1-x)= -x -x^2/2 -x^3/3 dla x bliskich 0
    
    vsubpd ymm0, ymm4, ymm0    ; YMM0 = 1.0 - YMM0
    
    vmulpd ymm1, ymm0, ymm0     ; YMM1 = (1 - x)^2
    vmulpd ymm2, ymm1, ymm0     ; YMM2 = (1 - x)^3

    vsubpd ymm0, ymm3, ymm0     ;YMM0 = -(1-x)

    vaddpd ymm3, ymm4, ymm4     ; YMM3 = 2.0
    vdivpd ymm1, ymm1, ymm3     ; YMM1 = (1 - x)^2 / 2
    vsubpd ymm0, ymm0, ymm1

    vaddpd ymm3, ymm3, ymm4     ; ymm3 = 3.0
    vdivpd ymm2, ymm2, ymm3     ; YMM2 = (1 - x)^3 / 3
    vsubpd ymm0, ymm0, ymm2

    ;mnozenie przez gamma
    vmulpd ymm0, ymm0, ymm6     ;ymm0 = ln(x)/gamma

    ;ymm0 x: 255* exp(x) = 1 + x + x^2/2 + x^3 / 6

    vmulpd ymm1, ymm0, ymm0 ;x^2
    vmulpd ymm2, ymm1, ymm0 ;x^3

    vaddpd ymm0, ymm0, ymm4
    ;zostalo x^2/2 + x^3 / 6
    
    vaddpd ymm3, ymm4, ymm4
    vdivpd ymm1, ymm1, ymm3
    vaddpd ymm0, ymm0, ymm1

    ;x^3/6 ymm3 = 2

    vaddpd ymm3, ymm3, ymm4
    vaddpd ymm3, ymm3, ymm3

    vdivpd ymm2, ymm2, ymm3
    vaddpd ymm0, ymm0, ymm2

    ;wynik *255 normalizacja
    vmulpd ymm0, ymm0, ymm5

    

    ;xmm1 b4 i b3 xmm0 b2 i b1
    vextractf128 xmm1, ymm0, 1 
    vcvttpd2dq xmm0, xmm0
    vcvttpd2dq xmm1, xmm1
    
    ;do 16 bitow
    packssdw xmm0, xmm0
    packssdw xmm1, xmm1
    ;do byte
    packuswb xmm0, xmm0   
    packuswb xmm1, xmm1
    
    movd eax, xmm0
    mov [rcx], al
    mov [rcx + 1], ah

    movd eax, xmm1
    mov [rcx + 2], al
    mov [rcx + 3], ah

    add rcx, 4
    sub r8, 4
    jmp LoopStart

EndLoop:


    pop rsp
    pop rsi
    pop rdi
    pop rbp
    pop rbx

    vmovups ymm6, [rsp]
    add rsp, 32
    ret

MyProc1 ENDP
END