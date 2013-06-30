set r1, 90				// start location

loop:
	set r0, helloStr
	call printStr
	cmp r1, 31970
	jae halt
	jmp loop
	
/*
 * Prints a null terminated string.
 * R0: string location
 * R1: output location
 */
printStr:
	push r2 			// use r2 as temp storage for character
	printStrLoop:
		set r2, [r0]
		cmp r2, r2
		jz printStrEnd
		set [r1], r2
		inc r1
		set [r1], 128
		inc r1
		inc r0
		jmp printStrLoop
printStrEnd:
	pop r2
	ret
	
halt:
	jmp halt

helloStr:
	db "hello world ", 0
