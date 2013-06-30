set r0, 50			// write position

loop:
    set rf, 219 	// solid white character
	
	set r1, r0
	mod r1, 400 	// r1 has X
	set r2, r0
	div r2, 150 	// r2 has Y
	xor r1, r2		// xor together for the pattern
    
	shl r1, 8		// color is in upper 8 bits
    or rf, r1		// combine
	set [r0], rf 	// write
    add r0, 2
					/* asdasd */
    cmp r0, 31990
    jbe loop

halt:
    jmp halt