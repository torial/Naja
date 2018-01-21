

; Example of making 32-bit PE program as raw code and data

format PE GUI
entry start

section '.text' code readable executable

  start:
        push    
        call    [ExitProcess]

section '.idata' import data readable writeable

  dd 0,0,0,RVA kernel_name,RVA kernel_table
  dd 0,0,0,0,0

  kernel_table:
    ExitProcess dd RVA _ExitProcess
    dd 0

  kernel_name db 'KERNEL32.DLL',0

  _ExitProcess dw 0
    db 'ExitProcess',0

section '.reloc' fixups data readable discardable       ; needed for Win32s
                                                    