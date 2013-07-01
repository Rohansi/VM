/*
 *	rand
 *	Clobbered Registers:
 *		r1, r2
 *	Arguments:
 *		None
 *	Returns:
 *		r0			Random number
 */
rand:
	set		r1, [randX]
	set		r2, r1
	shl		r1, 5
	xor		r2, r1
	
	set		[randX], [randY]
	set		[randY], [randZ]
	set		[randZ], [randW]
	
	set		r0, [randW]
	set		r1, r0
	shr		r1, 9
	xor		r0, r1
	xor		r0, r2
	shr		r2, 4
	set		[randW], r0
	
	ret

randX:	db	0x32, 0x39
randY:	db	0x4E, 0x2D
randZ:	db	0x3D, 0xB5
randW:	db	0x9A, 0x5F
