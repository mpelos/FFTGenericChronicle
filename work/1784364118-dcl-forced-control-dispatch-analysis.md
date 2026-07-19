# DCL forced-control dispatcher analysis

Executable: `D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe`

## Proven static control split

The real-code resolver at `0x38BBFC` reads the current unit pointer and gives native
loss-of-control statuses explicit branches before ordinary control returns:

- effective Chicken (`unit+0x63 & 0x04`) enters the branch at `0x38BC37`;
- effective Confusion (`unit+0x62 & 0x04`) is tested at `0x38BC87`;
- effective Charm (`unit+0x62 & 0x10`) is tested at `0x38BCB2`;
- effective Berserk (`unit+0x63 & 0x08`) enters the branch at `0x38BDFE`;
- no Berserk falls through to the zero return at `0x38BF1B`.

The Chicken branch synchronously calls `0x38E11C`, treats `-1` as failure, clears a
`0x240`-byte planning block, and calls planner thunk `0x321390` with `ecx=0xFF`, `edx=1`.
The Berserk branch requests mode `2` through VM thunk `0x38D8E4`, selects/commits its
forced target through common resolver `0x320500`, and continues through the same planning
tail. These branches expose reusable native control surfaces; they do not prove that native
Chicken preserves a later ally/self action, so Fear's support-action allowance and its
enemy-target rejection remain separate gates.

The outer planning function enters through thunk `0x32091C` and calls this resolver at
trace RVA `0x1098B8B5`. It treats `-1` as failure, but any nonzero handled result jumps
directly to the function's zero-return epilogue; only resolver result zero continues into
the ordinary planning path. Chicken's selector thunk `0x38E11C` reaches trace function
`0x10D6F6CD`, whose successful tail writes the selected x/y/layer tuple to active unit
offsets `+0x4F/+0x50/+0x51`. Planner thunk `0x321390` then writes the winning route tuple
into its four-byte selection record. Native Chicken therefore owns a complete forced-plan
transaction and suppresses ordinary voluntary planning; it cannot be used unchanged as a
Fear carrier that must allow a later self/ally/item/defensive action.

The shippable Taunt fallback needs no new control branch: a one-target-turn DCL-owned
Berserk status rule reaches the existing `0x38BDFE` forced-aggression branch. Its resistance
formula is inverted on Brave and its duration remains owned by the generic target-turn
status-duration tracker.

## Disassembly

```text
0038BBFC: 48 89 5C 24 08               mov qword ptr [rsp + 8], rbx
0038BC01: 48 89 6C 24 10               mov qword ptr [rsp + 0x10], rbp
0038BC06: 57                           push rdi
0038BC07: 48 83 EC 20                  sub rsp, 0x20
0038BC0B: 80 3D 51 74 4E 01 00         cmp byte ptr [rip + 0x14e7451], 0
0038BC12: 48 8D 2D E7 43 C7 FF         lea rbp, [rip - 0x38bc19]
0038BC19: 74 0E                        je 0x14038bc29
0038BC1B: 80 3D EC 66 4E 01 00         cmp byte ptr [rip + 0x14e66ec], 0
0038BC22: 74 19                        je 0x14038bc3d
0038BC24: E9 9D 02 00 00               jmp 0x14038bec6
0038BC29: 48 8B 3D 70 72 4E 01         mov rdi, qword ptr [rip + 0x14e7270]
0038BC30: C6 05 96 68 4E 01 00         mov byte ptr [rip + 0x14e6896], 0
0038BC37: F6 47 63 04                  test byte ptr [rdi + 0x63], 4
0038BC3B: 74 47                        je 0x14038bc84
0038BC3D: E8 DA 24 00 00               call 0x14038e11c
0038BC42: 83 F8 FF                     cmp eax, -1
0038BC45: 75 0F                        jne 0x14038bc56
0038BC47: C6 05 C0 66 4E 01 00         mov byte ptr [rip + 0x14e66c0], 0
0038BC4E: 83 C8 FF                     or eax, 0xffffffff
0038BC51: E9 C7 02 00 00               jmp 0x14038bf1d
0038BC56: 33 D2                        xor edx, edx
0038BC58: 48 8D 0D F5 5D 4E 01         lea rcx, [rip + 0x14e5df5]
0038BC5F: 41 B8 40 02 00 00            mov r8d, 0x240
0038BC65: E8 B6 E7 23 00               call 0x1405ca420
0038BC6A: BA 01 00 00 00               mov edx, 1
0038BC6F: B9 FF 00 00 00               mov ecx, 0xff
0038BC74: E8 17 57 F9 FF               call 0x140321390
0038BC79: 8B 05 E5 66 4E 01            mov eax, dword ptr [rip + 0x14e66e5]
0038BC7F: E9 7C 02 00 00               jmp 0x14038bf00
0038BC84: 8A 47 62                     mov al, byte ptr [rdi + 0x62]
0038BC87: A8 04                        test al, 4
0038BC89: 74 27                        je 0x14038bcb2
0038BC8B: 33 C9                        xor ecx, ecx
0038BC8D: E8 52 1C 00 00               call 0x14038d8e4
0038BC92: 0F B6 0D 35 68 4E 01         movzx ecx, byte ptr [rip + 0x14e6835]
0038BC99: 48 8D 15 78 71 4E 01         lea rdx, [rip + 0x14e7178]
0038BCA0: 66 C1 E1 0A                  shl cx, 0xa
0038BCA4: 8B D8                        mov ebx, eax
0038BCA6: 66 09 0D 6B 71 4E 01         or word ptr [rip + 0x14e716b], cx
0038BCAD: E9 A4 01 00 00               jmp 0x14038be56
0038BCB2: A8 10                        test al, 0x10
0038BCB4: 0F 84 44 01 00 00            je 0x14038bdfe
0038BCBA: BA FF 00 00 00               mov edx, 0xff
0038BCBF: 33 C9                        xor ecx, ecx
```

```text
0038BDFE: F6 47 63 08                  test byte ptr [rdi + 0x63], 8
0038BE02: 0F 84 13 01 00 00            je 0x14038bf1b
0038BE08: B9 02 00 00 00               mov ecx, 2
0038BE0D: E8 D2 1A 00 00               call 0x14038d8e4
0038BE12: 8B D8                        mov ebx, eax
0038BE14: 8A 47 12                     mov al, byte ptr [rdi + 0x12]
0038BE17: 04 50                        add al, 0x50
0038BE19: 3C 2F                        cmp al, 0x2f
0038BE1B: 76 20                        jbe 0x14038be3d
0038BE1D: F6 47 63 02                  test byte ptr [rdi + 0x63], 2
0038BE21: 75 1A                        jne 0x14038be3d
0038BE23: 0F B6 05 A5 66 4E 01         movzx eax, byte ptr [rip + 0x14e66a5]
0038BE2A: 48 69 D0 88 00 00 00         imul rdx, rax, 0x88
0038BE31: 48 8D 05 60 67 4E 01         lea rax, [rip + 0x14e6760]
0038BE38: 48 03 D0                     add rdx, rax
0038BE3B: EB 19                        jmp 0x14038be56
0038BE3D: 0F B6 05 8A 66 4E 01         movzx eax, byte ptr [rip + 0x14e668a]
0038BE44: 48 8D 15 D1 6F 4E 01         lea rdx, [rip + 0x14e6fd1]
0038BE4B: 66 C1 E0 0A                  shl ax, 0xa
0038BE4F: 66 09 05 C6 6F 4E 01         or word ptr [rip + 0x14e6fc6], ax
0038BE56: 48 8B CA                     mov rcx, rdx
0038BE59: E8 A2 46 F9 FF               call 0x140320500
0038BE5E: 33 C0                        xor eax, eax
0038BE60: 0F 57 C0                     xorps xmm0, xmm0
0038BE63: 89 05 D4 64 4E 01            mov dword ptr [rip + 0x14e64d4], eax
0038BE69: 88 05 D2 64 4E 01            mov byte ptr [rip + 0x14e64d2], al
0038BE6F: 48 63 C3                     movsxd rax, ebx
0038BE72: 0F 11 05 B4 64 4E 01         movups xmmword ptr [rip + 0x14e64b4], xmm0
0038BE79: C6 84 28 2D 23 87 01 01      mov byte ptr [rax + rbp + 0x187232d], 1
0038BE81: E8 AA 00 00 00               call 0x14038bf30
0038BE86: 85 C0                        test eax, eax
0038BE88: 0F 85 86 00 00 00            jne 0x14038bf14
0038BE8E: 48 63 CB                     movsxd rcx, ebx
0038BE91: 48 C1 E1 09                  shl rcx, 9
0038BE95: C6 05 7B 64 4E 01 00         mov byte ptr [rip + 0x14e647b], 0
0038BE9C: 8A 84 29 2F 3D 85 01         mov al, byte ptr [rcx + rbp + 0x1853d2f]
0038BEA3: 88 05 6B 64 4E 01            mov byte ptr [rip + 0x14e646b], al
0038BEA9: 8A 84 29 30 3D 85 01         mov al, byte ptr [rcx + rbp + 0x1853d30]
0038BEB0: 88 05 60 64 4E 01            mov byte ptr [rip + 0x14e6460], al
0038BEB6: 8A 84 29 31 3D 85 01         mov al, byte ptr [rcx + rbp + 0x1853d31]
0038BEBD: C0 E8 07                     shr al, 7
0038BEC0: 88 05 4F 64 4E 01            mov byte ptr [rip + 0x14e644f], al
0038BEC6: 48 8D 0D 47 64 4E 01         lea rcx, [rip + 0x14e6447]
0038BECD: E8 BE 1F 00 00               call 0x14038de90
0038BED2: 83 F8 FF                     cmp eax, -1
0038BED5: 75 0C                        jne 0x14038bee3
0038BED7: C6 05 30 64 4E 01 01         mov byte ptr [rip + 0x14e6430], 1
0038BEDE: E9 6B FD FF FF               jmp 0x14038bc4e
0038BEE3: BA 01 00 00 00               mov edx, 1
0038BEE8: B9 FF FF FF 7F               mov ecx, 0x7fffffff
0038BEED: E8 9E 54 F9 FF               call 0x140321390
0038BEF2: 0F B6 05 D4 65 4E 01         movzx eax, byte ptr [rip + 0x14e65d4]
0038BEF9: 8B 84 85 64 23 87 01         mov eax, dword ptr [rbp + rax*4 + 0x1872364]
0038BF00: C6 05 AA 6F 4E 01 00         mov byte ptr [rip + 0x14e6faa], 0
0038BF07: C6 05 9A 6F 4E 01 00         mov byte ptr [rip + 0x14e6f9a], 0
0038BF0E: 89 05 98 6F 4E 01            mov dword ptr [rip + 0x14e6f98], eax
0038BF14: B8 01 00 00 00               mov eax, 1
0038BF19: EB 02                        jmp 0x14038bf1d
0038BF1B: 33 C0                        xor eax, eax
0038BF1D: 48 8B 5C 24 30               mov rbx, qword ptr [rsp + 0x30]
0038BF22: 48 8B 6C 24 38               mov rbp, qword ptr [rsp + 0x38]
```

```text
1098B8AB: 48 8D 35 76 77 EE F0         lea rsi, [rip - 0xf11888a]
1098B8B2: 83 CB FF                     or ebx, 0xffffffff
1098B8B5: E8 42 03 A0 EF               call 0x14038bbfc
1098B8BA: 39 D8                        cmp eax, ebx
1098B8BC: 75 0E                        jne 0x15098b8cc
1098B8BE: 44 88 25 48 6A EE F0         mov byte ptr [rip - 0xf1195b8], r12b
1098B8C5: 89 D8                        mov eax, ebx
1098B8C7: E9 90 00 00 00               jmp 0x15098b95c
1098B8CC: 85 C0                        test eax, eax
1098B8CE: 0F 85 86 00 00 00            jne 0x15098b95a
1098B8D4: 48 8B 05 C5 75 EE F0         mov rax, qword ptr [rip - 0xf118a3b]
1098B8DB: 48 8B 15 F6 6B EE F0         mov rdx, qword ptr [rip - 0xf11940a]
1098B8E2: F6 40 63 03                  test byte ptr [rax + 0x63], 3
1098B8E6: 74 09                        je 0x15098b8f1
1098B8E8: F6 05 68 6A EE F0 40         test byte ptr [rip - 0xf119598], 0x40
1098B8EF: 75 14                        jne 0x15098b905
1098B8F1: F6 40 65 04                  test byte ptr [rax + 0x65], 4
1098B8F5: 0F 84 A3 FD FF FF            je 0x15098b69e
1098B8FB: F6 42 04 40                  test byte ptr [rdx + 4], 0x40
1098B8FF: 0F 84 99 FD FF FF            je 0x15098b69e
1098B905: 48 89 F1                     mov rcx, rsi
1098B908: 48 29 F2                     sub rdx, rsi
1098B90B: 0F B7 04 0A                  movzx eax, word ptr [rdx + rcx]
1098B90F: 66 89 01                     mov word ptr [rcx], ax
1098B912: 48 83 C1 02                  add rcx, 2
1098B916: 48 8D 05 1B 77 EE F0         lea rax, [rip - 0xf1188e5]
1098B91D: 48 39 C1                     cmp rcx, rax
1098B920: 7C E9                        jl 0x15098b90b
1098B922: E8 31 1D A0 EF               call 0x14038d658
1098B927: 39 D8                        cmp eax, ebx
1098B929: 75 09                        jne 0x15098b934
1098B92B: C6 05 DB 69 EE F0 02         mov byte ptr [rip - 0xf119625], 2
1098B932: EB 91                        jmp 0x15098b8c5
1098B934: 48 8B 0D 9D 6B EE F0         mov rcx, qword ptr [rip - 0xf119463]
1098B93B: BA 40 8B B5 78               mov edx, 0x78b58b40
1098B940: 33 15 62 FB 75 04            xor edx, dword ptr [rip + 0x475fb62]
1098B946: 48 29 CE                     sub rsi, rcx
1098B949: 0F B7 04 0E                  movzx eax, word ptr [rsi + rcx]
1098B94D: 66 89 01                     mov word ptr [rcx], ax
1098B950: 48 8D 49 02                  lea rcx, [rcx + 2]
1098B954: 48 83 EA 01                  sub rdx, 1
1098B958: 75 EF                        jne 0x15098b949
1098B95A: 31 C0                        xor eax, eax
```

```text
10D6F81E: 8A 05 AC 2C B0 F0            mov al, byte ptr [rip - 0xf4fd354]
10D6F824: 48 8B 0D 75 36 B0 F0         mov rcx, qword ptr [rip - 0xf4fc98b]
10D6F82B: 88 41 4F                     mov byte ptr [rcx + 0x4f], al
10D6F82E: 8A 05 9E 2C B0 F0            mov al, byte ptr [rip - 0xf4fd362]
10D6F834: 48 8B 0D 65 36 B0 F0         mov rcx, qword ptr [rip - 0xf4fc99b]
10D6F83B: 88 41 50                     mov byte ptr [rcx + 0x50], al
10D6F83E: 48 8B 15 5B 36 B0 F0         mov rdx, qword ptr [rip - 0xf4fc9a5]
10D6F845: 8A 0D 86 2C B0 F0            mov cl, byte ptr [rip - 0xf4fd37a]
10D6F84B: C0 E1 07                     shl cl, 7
10D6F84E: 8A 42 51                     mov al, byte ptr [rdx + 0x51]
10D6F851: 24 7F                        and al, 0x7f
10D6F853: 08 C1                        or cl, al
10D6F855: 88 4A 51                     mov byte ptr [rdx + 0x51], cl
10D6F858: 31 C0                        xor eax, eax
10D6F85A: 48 8B 5C 24 40               mov rbx, qword ptr [rsp + 0x40]
10D6F85F: 48 8B 6C 24 48               mov rbp, qword ptr [rsp + 0x48]
10D6F864: 48 8B 74 24 50               mov rsi, qword ptr [rsp + 0x50]
10D6F869: 48 83 C4 20                  add rsp, 0x20
10D6F86D: 41 5F                        pop r15
10D6F86F: 41 5E                        pop r14
10D6F871: 5F                           pop rdi
10D6F872: C3                           ret
```
