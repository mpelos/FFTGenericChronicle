# DCL Fear native-boundary analysis

Executable: `D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe`

## Strong static boundary

Battle state `0x19` calls the voluntary target-input handler at `0x20C30C`.
Only its accepted-confirm path reaches the call at `0x20C55F`; that call enters
thunk `0x2072F8`, whose real body writes battle state `0x1B`. A guarded replacement
of this single call can therefore reject a Fear-invalid player confirmation while
leaving the target-selection state active. Reaction queue/delivery states `0x29` and
`0x2C` do not traverse this voluntary confirmation boundary.

The universal action-result path calls the affected-target builder at `0x281EC3`.
The complete native target list is available at `0x281EC8`, before publication.
Entries replaced with `0xFF` are skipped both by the per-target result call and by
the affected-target output copy. This boundary sees unit, tile and area targeting
after native target expansion rather than guessing from cursor coordinates.

The implementable Fear policy is therefore: inspect the completed list; reject the
whole candidate when any affected unit is opposing; leave self, ally, empty-tile and
defensive candidates intact; apply invalidation during AI evaluation and fail-closed
execution, but never during reaction delivery. Player forecast records the decision
and the voluntary-confirm hook enforces it without touching reactions.

## Voluntary confirmation disassembly

```text
0020C520: 64 01 45 33                  add dword ptr fs:[rbp + 0x33], eax
0020C524: C9                           leave
0020C525: 33 D2                        xor edx, edx
0020C527: 45 8D 41 15                  lea r8d, [r9 + 0x15]
0020C52B: E8 68 00 EF FF               call 0x1400fc598
0020C530: 84 C0                        test al, al
0020C532: 75 06                        jne 0x14020c53a
0020C534: 0F B7 4B 0C                  movzx ecx, word ptr [rbx + 0xc]
0020C538: EB 07                        jmp 0x14020c541
0020C53A: 66 89 7B 0C                  mov word ptr [rbx + 0xc], di
0020C53E: 0F B7 CF                     movzx ecx, di
0020C541: 41 8D 04 0E                  lea eax, [r14 + rcx]
0020C545: 66 89 43 0C                  mov word ptr [rbx + 0xc], ax
0020C549: 66 3B CF                     cmp cx, di
0020C54C: 76 16                        jbe 0x14020c564
0020C54E: 8B 05 3C E3 B2 00            mov eax, dword ptr [rip + 0xb2e33c]
0020C554: 89 05 5A ED A5 00            mov dword ptr [rip + 0xa5ed5a], eax
0020C55A: E8 85 E4 FE FF               call 0x1401fa9e4
0020C55F: E8 94 AD FF FF               call 0x1402072f8
0020C564: 83 3D 61 EC A5 00 19         cmp dword ptr [rip + 0xa5ec61], 0x19
0020C56B: 74 1F                        je 0x14020c58c
0020C56D: 48 8B 05 34 D8 AC 03         mov rax, qword ptr [rip + 0x3acd834]
0020C574: 48 8B 48 10                  mov rcx, qword ptr [rax + 0x10]
```

## Affected-target sweep disassembly

```text
00281EBC: 49 8B D6                     mov rdx, r14
00281EBF: 48 8D 4D D8                  lea rcx, [rbp - 0x28]
00281EC3: E8 8C 08 00 00               call 0x140282754
00281EC8: 44 8A 65 D0                  mov r12b, byte ptr [rbp - 0x30]
00281ECC: 0F 10 45 D8                  movups xmm0, xmmword ptr [rbp - 0x28]
00281ED0: 88 05 B9 E8 52 00            mov byte ptr [rip + 0x52e8b9], al
00281ED6: 48 8D 05 23 E1 D7 FF         lea rax, [rip - 0x281edd]
00281EDD: 4C 8D B0 80 3E 85 01         lea r14, [rax + 0x1853e80]
00281EE4: 4C 03 F6                     add r14, rsi
00281EE7: 49 8B CE                     mov rcx, r14
00281EEA: F3 0F 7F 05 3E 91 5E 01      movdqu xmmword ptr [rip + 0x15e913e], xmm0
00281EF2: E8 D1 79 08 00               call 0x1403098c8
00281EF7: 49 8B DD                     mov rbx, r13
00281EFA: 8A 54 1D D8                  mov dl, byte ptr [rbp + rbx - 0x28]
00281EFE: 80 FA FF                     cmp dl, 0xff
00281F01: 74 0F                        je 0x140281f12
00281F03: 49 8B CE                     mov rcx, r14
00281F06: 44 88 2D 7F E8 52 00         mov byte ptr [rip + 0x52e87f], r13b
00281F0D: E8 9A 7A 08 00               call 0x1403099ac
00281F12: 48 FF C3                     inc rbx
00281F15: 48 83 FB 15                  cmp rbx, 0x15
00281F19: 7C DF                        jl 0x140281efa
00281F1B: 41 B9 10 00 00 00            mov r9d, 0x10
00281F21: 4C 8D 1D D8 E0 D7 FF         lea r11, [rip - 0x281f28]
00281F28: 42 80 BC 1E 81 3E 85 01 12   cmp byte ptr [rsi + r11 + 0x1853e81], 0x12
00281F31: 48 8D 05 88 90 5E 01         lea rax, [rip + 0x15e9088]
00281F38: 48 89 05 21 90 5E 01         mov qword ptr [rip + 0x15e9021], rax
00281F3F: 41 8D 59 F1                  lea ebx, [r9 - 0xf]
00281F43: 45 8D 71 F8                  lea r14d, [r9 - 8]
00281F47: 75 1E                        jne 0x140281f67
00281F49: 46 84 8C 1E D1 3E 85 01      test byte ptr [rsi + r11 + 0x1853ed1], r9b
00281F51: 74 14                        je 0x140281f67
00281F53: 44 08 35 8D 90 5E 01         or byte ptr [rip + 0x15e908d], r14b
00281F5A: 44 08 0D 83 90 5E 01         or byte ptr [rip + 0x15e9083], r9b
00281F61: 88 1D 59 90 5E 01            mov byte ptr [rip + 0x15e9059], bl
00281F67: 48 8D 57 02                  lea rdx, [rdi + 2]
00281F6B: 45 8B C5                     mov r8d, r13d
00281F6E: 4C 8D 15 BB 90 5E 01         lea r10, [rip + 0x15e90bb]
00281F75: 4C 2B D2                     sub r10, rdx
00281F78: 41 8A 0C 12                  mov cl, byte ptr [r10 + rdx]
00281F7C: 41 8D 40 01                  lea eax, [r8 + 1]
00281F80: 80 F9 FF                     cmp cl, 0xff
00281F83: 88 0A                        mov byte ptr [rdx], cl
00281F85: 41 0F 44 C0                  cmove eax, r8d
00281F89: 48 03 D3                     add rdx, rbx
00281F8C: 44 8B C0                     mov r8d, eax
```
