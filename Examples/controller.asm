set [printLocation], 800

in r0, 100				// controller port
jz notFound

set r0, msgControllerFound
call printStr

loop:
	in r0, 100			// read status
upButton:
	set [880], " \xFF"	// reset display, causes flicker but who cares
	and 1, r0			// test bit 1 (yes this is valid)
	jz downButton		// off? skip
	set [880], "\x1E\xFF"
downButton:
	set [882], " \xFF"
	and 2, r0
	jz leftButton
	set [882], "\x1F\xFF"
leftButton:
	set [884], " \xFF"	// string literals like this can only be one or two bytes
	and 4, r0
	jz rightButton
	set [884], "\x11\xFF"
rightButton:
	set [886], " \xFF"
	and 8, r0
	jz aButton
	set [886], "\x10\xFF"
aButton:
	set [888], " \xFF"
	and 16, r0
	jz bButton
	set [888], "A\xFF"
bButton:
	set [890], " \xFF"
	and 32, r0
	jz cButton
	set [890], "B\xFF"
cButton:
	set [892], " \xFF"
	and 64, r0
	jz skip
	set [892], "C\xFF"
skip:
	jmp loop

notFound:
	set r0, msgControllerNotFound
	call printStr
	jmp halt

msgControllerFound:
	db "Found controller! ", 0
msgControllerNotFound:
	db "Controller not found! ", 0

/*
 * Prints a null terminated string.
 * R0: string location
 */
printStr:
	push r1	// r1 for write position
	push r2 // r2 as temp storage for character
	set r1, [printLocation]
	printStrLoop:
		set r2, [r0]
		and r2, 255
		cmp r2, r2
		jz printStrEnd
		
		set [r1], r2	// write character
		inc r1
		set [r1], 255	// write color (white)
		inc r1
		
		inc r0
		jmp printStrLoop
printStrEnd:
	set [printLocation], r1
	pop r2
	pop r1
	ret
	
printLocation:
	db 0, 0
	
halt:
	jmp halt