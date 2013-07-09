set rf, testStr
call tvgaPuts

loop:
	in rf, 150
	jz loop
	call tvgaPutc
	jmp loop
	
halt:
	jmp halt
	
testStr:
	db "Test keyboard: ", 0

/* 80x25 text display at 10,20 */
// rf - null terminated string
tvgaPuts:
		push re		// temp
		
	tvgaPutsLoop:
		set re, [rf]
		and re, 255
		jz tvgaPutsEnd
		inc rf
		
		push rf			// 2fast
		set rf, re
		call tvgaPutc
		pop rf
		jmp tvgaPutsLoop
		
	tvgaPutsEnd:
		pop re
		ret

// rf - number to print
tvgaPutn:
		push re		// buffer ptr
		push rd		// temp
		push rc		// temp
		push rb		// digit counter
		
		set re, tvgaPutnBuffer
		xor rb, rb
		
		push rf
		cmp rf, 0
		jae tvgaPutnZero
		sub rc, rf
		set rf, rc
	tvgaPutnZero:
		cmp rf, rf
		jnz tvgaPutnConvert
		set [re], "0"
		inc re
		jmp tvgaPutnEnd
		
	tvgaPutnConvert:
		cmp rf, rf
		jz tvgaPutnNeg
		set rd, rf
		mod rd, 10
		add rd, "0"
		set [re], rd
		inc re
		div rf, 10
		inc rb
		jmp tvgaPutnConvert
		
	tvgaPutnNeg:
		pop rf
		cmp rf, 0
		jae tvgaPutnReverse
		set [re], "-"
		inc re
		inc rb
	
	tvgaPutnReverse:
		push rb
		push ra
		set rd, tvgaPutnBuffer
		set re, tvgaPutnBuffer2
		add re, rb
		dec re
		tvgaPutnReverseLoop:
			set rc, [rd]
			and rc, 255
			shl ra, 8
			or ra, rc
			set [re], ra
			inc rd
			dec re
			dec rb
			jnz tvgaPutnReverseLoop
		pop ra
		pop rb
		
	tvgaPutnEnd:
		set rf, tvgaPutnBuffer2
		call tvgaPuts
		
		pop rb
		pop rc
		pop rd
		pop re
		ret
		
tvgaPutnBuffer:
	db "ABCDEF", 0, 0
tvgaPutnBuffer2:
	db "ABCDEF", 0, 0

// rf - character
tvgaPutc:
		push re		// cursor X
		push rd		// cursor Y
		push rc		// temp
		push rb		// temp
		set re, [tvgaX]
		set rd, [tvgaY]
		and rf, 255
		
	tvgaPutcBackspaceCheck:
		cmp rf, "\b"
		jne tvgaPutcXCheck
		dec re
		cmp re, 0
		jae tvgaPutcBackspaceClear
	tvgaPutcBackspaceUpLine:
		set re, 79				// eol = 80 - 1
		dec rd
		cmp rd, 0
		jae tvgaPutcBackspaceClear
		xor re, re
		xor rd, rd
	tvgaPutcBackspaceClear:
		set rc, rd				// row = (20 + y) * 400
		add rc, 20
		mul rc, 400
		set rb, re				// column = (10 + x) * 2
		add rb, 10
		mul rb, 2
		add rc, rb
		set [rc], 0xFF00
		jmp tvgaPutcEnd
	
	tvgaPutcXCheck:
		cmp re, 80
		jb tvgaPutcYCheck
		xor re, re
		inc rd
		
	tvgaPutcYCheck:
		cmp rd, 25
		jb tvgaPutcNewlineCheck
		call tvgaScroll
		dec rd
	
	tvgaPutcNewlineCheck:
		cmp rf, "\n"
		jne tvgaPutcWrite
		xor re, re
		inc rd
		cmp rd, 25
		jb tvgaPutcEnd
		call tvgaScroll
		dec rd
		jmp tvgaPutcEnd
		
	tvgaPutcWrite:
		set rc, rd				// row = (20 + y) * 400
		add rc, 20
		mul rc, 400
		set rb, re				// column = (10 + x) * 2
		add rb, 10
		mul rb, 2
		add rc, rb
		or rf, 0xFF00
		set [rc], rf
		inc re
		
	tvgaPutcEnd:
		set [tvgaX], re
		set [tvgaY], rd
		pop rb
		pop rc
		pop rd
		pop re
		ret

tvgaScroll:
		push rf		// line counter
		push re		// dest
		push rd		// source
		push rc		// char counter
		
		set re, 8020			// (20 * 400) + (10 * 2)
		set rd, 8420			// ((20 + 1) * 400) + (10 * 2)
		
		set rf, 24
	tvgaScrollLoop:
	
		set rc, 80
		tvgaScrollCopy:
			set [re], [rd]
			add re, 2
			add rd, 2
			dec rc
			jnz tvgaScrollCopy
			
		add re, 240				// 400 - (80 * 2)
		add rd, 240
		
		dec rf
		jnz tvgaScrollLoop
		
		set rc, 80
	tvgaScrollClearLine:
		set [re], 0
		add re, 2
		dec rc
		jnz tvgaScrollClearLine
		
		pop rc
		pop rd
		pop re
		pop rf
		ret

/* Cursor */
tvgaX: db 0, 0
tvgaY: db 0, 0