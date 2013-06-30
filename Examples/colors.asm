set rf, 100			// end of program

loop:
	set rd, 0x08DB
	call changeAll
	set rd, 0x10DB
	call changeAll
	set rd, 0x20DB
	call changeAll
	set rd, 0x40DB
	call changeAll
	jmp loop

changeAll:
	push rf
	
changeAllLoop:
	in re, 100
	set [rf], rd
	add rf, 2
	cmp rf, 31970
	jae changeAllDone
	jmp changeAllLoop
	
changeAllDone:
	pop rf
	ret