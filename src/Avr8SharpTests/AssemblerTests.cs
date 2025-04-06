using AVR8Sharp.Utils;
namespace AVr8SharpTests;

[TestFixture]
public class Assembler
{
	[Test(Description = "The assembler should correctly assemble the ADD instruction")]
	public void ADD ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ADD r16, r11");
		Assert.Multiple(() =>
		{
			Assert.That(assembler.Errors, Is.Empty);
			Assert.That(assembler.Lines, Has.Count.EqualTo(1));
			
			var line = assembler.Lines[0];
			Assert.That(line.BytesOffset, Is.EqualTo(0));
			Assert.That(line.Bytes, Is.EqualTo("0d0b"));
			Assert.That(line.Line, Is.EqualTo(1));
			Assert.That(line.Text, Is.EqualTo("ADD r16, r11"));
			
			Assert.That(assembler.Labels, Is.Empty);
			
			var expected = Bytes ("0b0d");
			Assert.That(result, Is.EqualTo(expected));
		});
	}

	[Test(Description = "The assembler should correctly support labels")]
	public void Labels ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("loop: JMP loop");

		Assert.That(Bytes ("0c940000"), Is.EqualTo(result));
	}
	
	[Test(Description = "The assembler should correctly support multi-line code")]
	public void MultiLine ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble (@"
      start: 
      LDI r16, 15
      EOR r16, r0
      BREQ start
");
		Assert.That(Bytes ("0fe00025e9f3"), Is.EqualTo(result));
	}
	
	[Test(Description = "The assembler should successfully assemble an empty program")]
	public void Empty ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("");
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Empty);
            Assert.That(assembler.Errors, Is.Empty);
            Assert.That(assembler.Lines, Is.Empty);
            Assert.That(assembler.Labels, Is.Empty);
        });
    }
	
	[Test(Description = "The assembler should return an empty result when an error occurs")]
	public void Empty_When_Error ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LDI r15, 20");
		Assert.Multiple(() =>
		{
			Assert.That(result, Is.Empty);
			
			Assert.That(assembler.Errors, Has.Count.EqualTo(1));
			Assert.That(assembler.Errors[0], Is.EqualTo("Line 0: Rd out of range: 16<>31"));
			
			Assert.That(assembler.Lines, Is.Empty);
			Assert.That(assembler.Labels, Is.Empty);
		});
	}
	
	[Test(Description = "The assembler should correctly assemble the ADC instruction")]
	public void ADC ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ADC r0, r1");
		
		Assert.That (Bytes ("011c"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the BCLR instruction")]
	public void BCLR ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("BCLR 2");
		
		Assert.That (Bytes ("a894"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the BLD instruction")]
	public void BLD ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("BLD r4, 7");
		
		Assert.That (Bytes ("47f8"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the BRBC instruction")]
	public void BRBC ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("BRBC 0, +8");
		
		Assert.That (Bytes ("20f4"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the BRBS 3, 92 instruction")]
	public void BRBS ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("BRBS 3, 92");
		
		Assert.That (Bytes ("73f1"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the BRBS 3, -4 instruction")]
	public void BRBS_Negative ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("BRBS 3, -4");
		
		Assert.That (Bytes ("f3f3"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the BREQ instruction with forward label target")]
	public void BREQ ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("BREQ next \n next:");
		
		Assert.That (Bytes ("01f0"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the BRGE instruction with forward label target")]
	public void BRNE ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("BRNE next \n next:");
		
		Assert.That (Bytes ("01f4"), Is.EqualTo (result));
	}

	[Test(Description = "The assembler should correctly assemble the CBI instruction")]
	public void CBI ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("CBI 0xc, 5");
		
		Assert.That (Bytes ("6598"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the CALL instruction")]
	public void CALL ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("CALL 0xb8");
		
		Assert.That (Bytes ("0e945c00"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the CPC r27, r18 instruction")]
	public void CPC ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("CPC r27, r18");
		
		Assert.That (Bytes ("b207"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the CPC r24, r1 instruction")]
	public void CPC_2 ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("CPC r24, r1");
		
		Assert.That (Bytes ("8105"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the CPI instruction")]
	public void CPI ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("CPI r26, 0x9");
		
		Assert.That (Bytes ("a930"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the CPSE instruction")]
	public void CPSE ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("CPSE r2, r3");
		
		Assert.That (Bytes ("2310"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ICALL instruction")]
	public void ICALL ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ICALL");
		
		Assert.That (Bytes ("0995"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the IJMP instruction")]
	public void IJMP ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("IJMP");
		
		Assert.That (Bytes ("0994"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the IN instruction")]
	public void IN ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("IN r5, 0xb");
		
		Assert.That (Bytes ("5bb0"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the INC instruction")]
	public void INC ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("INC r5");
		
		Assert.That (Bytes ("5394"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the JMP instruction")]
	public void JMP ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("JMP 0xb8");
		
		Assert.That (Bytes ("0c945c00"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LAC instruction")]
	public void LAC ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LAC Z, r19");
		
		Assert.That (Bytes ("3693"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LAS instruction")]
	public void LAS ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LAS Z, r17");
		
		Assert.That (Bytes ("1593"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LAT instruction")]
	public void LAT ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LAT Z, r0");
		
		Assert.That (Bytes ("0792"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LDI instruction")]
	public void LDI ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LDI r28, 0xff");
		
		Assert.That (Bytes ("cfef"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LDS instruction")]
	public void LDS ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LDS r5, 0x150");
		
		Assert.That (Bytes ("50905001"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r1, X instruction")]
	public void LD ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r1, X");
		
		Assert.That (Bytes ("1c90"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r17, X+ instruction")]
	public void LD_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r17, X+");
		
		Assert.That (Bytes ("1d91"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r1, -X instruction")]
	public void LD_Minus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r1, -X");
		
		Assert.That (Bytes ("1e90"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r8, Y instruction")]
	public void LD_Y ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r8, Y");
		
		Assert.That (Bytes ("8880"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r3, Y+ instruction")]
	public void LD_Y_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r3, Y+");
		
		Assert.That (Bytes ("3990"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r0, -Y instruction")]
	public void LD_Y_Minus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r0, -Y");
		
		Assert.That (Bytes ("0a90"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LDD r4, Y+2 instruction")]
	public void LDD ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LDD r4, Y+2");
		
		Assert.That (Bytes ("4a80"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r5, Z instruction")]
	public void LD_Z ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r5, Z");
		
		Assert.That (Bytes ("5080"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r7, Z+ instruction")]
	public void LD_Z_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r7, Z+");
		
		Assert.That (Bytes ("7190"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LD r0, -Z instruction")]
	public void LD_Z_Minus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LD r0, -Z");
		
		Assert.That (Bytes ("0290"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LDD r15, Z+31 instruction")]
	public void LDD_Z_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LDD r15, Z+31");
		
		Assert.That (Bytes ("f78c"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LPM instruction")]
	public void LPM ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LPM");
		
		Assert.That (Bytes ("c895"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LPM r2, Z instruction")]
	public void LPM_Z ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LPM r2, Z");
		
		Assert.That (Bytes ("2490"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LPM r1, Z+ instruction")]
	public void LPM_Z_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LPM r1, Z+");
		
		Assert.That (Bytes ("1590"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the LSR r7 instruction")]
	public void LSR ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("LSR r7");
		
		Assert.That (Bytes ("7694"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the MOV r7, r8 instruction")]
	public void MOV ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("MOV r7, r8");
		
		Assert.That (Bytes ("782c"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the MOVW r26, r22 instruction")]
	public void MOVW ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("MOVW r26, r22");
		
		Assert.That (Bytes ("db01"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the MUL r5, r6 instruction")]
	public void MUL ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("MUL r5, r6");
		
		Assert.That (Bytes ("569c"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the MULS r18, r19 instruction")]
	public void MULS ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("MULS r18, r19");
		
		Assert.That (Bytes ("2302"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the MULSU r16, r17 instruction")]
	public void MULSU ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("MULSU r16, r17");
		
		Assert.That (Bytes ("0103"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the NEG r20 instruction")]
	public void NEG ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("NEG r20");
		
		Assert.That (Bytes ("4195"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the NOP instruction")]
	public void NOP ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("NOP");
		
		Assert.That (Bytes ("0000"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the OR r5, r2 instruction")]
	public void OR ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("OR r5, r2");
		
		Assert.That (Bytes ("5228"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ORI r22, 0x81 instruction")]
	public void ORI ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ORI r22, 0x81");
		
		Assert.That (Bytes ("6168"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the OUT instruction")]
	public void OUT ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("OUT 0x3f, r1");
		
		Assert.That (Bytes ("1fbe"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the POP instruction")]
	public void POP ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("POP r26");
		
		Assert.That (Bytes ("af91"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the PUSH instruction")]
	public void PUSH ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("PUSH r11");
		
		Assert.That (Bytes ("bf92"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the RCALL instruction")]
	public void RCALL ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("RCALL +6");
		
		Assert.That (Bytes ("03d0"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the RCALL -4 instruction")]
	public void RCALL_Negative ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("RCALL -4");
		
		Assert.That (Bytes ("fedf"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the RET instruction")]
	public void RET ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("RET");
		
		Assert.That (Bytes ("0895"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the RETI instruction")]
	public void RETI ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("RETI");
		
		Assert.That (Bytes ("1895"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the RJMP 2 instruction")]
	public void RJMP ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("RJMP 2");
		
		Assert.That (Bytes ("01c0"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ROR r0 instruction")]
	public void ROR ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ROR r0");
		
		Assert.That (Bytes ("0794"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the SBCI r23, 3 instruction")]
	public void SBC ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("SBCI r23, 3");
		
		Assert.That (Bytes ("7340"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the SBI 0x0c, 5 instruction")]
	public void SBI ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("SBI 0x0c, 5");
		
		Assert.That (Bytes ("659a"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the SBIS 0x0c, 5 instruction")]
	public void SBIS ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("SBIS 0x0c, 5");
		
		Assert.That (Bytes ("659b"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the SBIW r28, 2 instruction")]
	public void SBIW ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("SBIW r28, 2");
		
		Assert.That (Bytes ("2297"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the SLEEP instruction")]
	public void SLEEP ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("SLEEP");
		
		Assert.That (Bytes ("8895"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the SPM instruction")]
	public void SPM ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("SPM");
		
		Assert.That (Bytes ("e895"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the SPM Z+ instruction")]
	public void SPM_Z_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("SPM Z+");
		
		Assert.That (Bytes ("f895"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the STS 0x151, r31 instruction")]
	public void STS ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("STS 0x151, r31");
		
		Assert.That (Bytes ("f0935101"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST X, r1 instruction")]
	public void ST ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST X, r1");
		
		Assert.That (Bytes ("1c92"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST X+, r1 instruction")]
	public void ST_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST X+, r1");
		
		Assert.That (Bytes ("1d92"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST -X, r17 instruction")]
	public void ST_Minus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST -X, r17");
		
		Assert.That (Bytes ("1e93"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST Y, r2 instruction")]
	public void ST_Y ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST Y, r2");
		
		Assert.That (Bytes ("2882"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST Y+, r1 instruction")]
	public void ST_Y_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST Y+, r1");
		
		Assert.That (Bytes ("1992"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST -Y, r1 instruction")]
	public void ST_Y_Minus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST -Y, r1");
		
		Assert.That (Bytes ("1a92"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the STD Y+17, r0 instruction")]
	public void STD_Y_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("STD Y+17, r0");
		
		Assert.That (Bytes ("098a"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST Z, r16 instruction")]
	public void ST_Z ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST Z, r16");
		
		Assert.That (Bytes ("0083"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST Z+, r0 instruction")]
	public void ST_Z_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST Z+, r0");
		
		Assert.That (Bytes ("0192"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the ST -Z, r16 instruction")]
	public void ST_Z_Minus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("ST -Z, r16");
		
		Assert.That (Bytes ("0293"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the STD Z+1, r0 instruction")]
	public void STD_Z_Plus ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("STD Z+1, r0");
		
		Assert.That (Bytes ("0182"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the SWAP r1 instruction")]
	public void SWAP ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("SWAP r1");
		
		Assert.That (Bytes ("1294"), Is.EqualTo (result));
	}
	
	[Test(Description = "The assembler should correctly assemble the XCH Z, r21 instruction")]
	public void XCH ()
	{
		var assembler = new AvrAssembler ();
		var result = assembler.Assemble ("XCH Z, r21");
		
		Assert.That (Bytes ("5493"), Is.EqualTo (result));
	}
	
	private byte[] Bytes (string hex)
	{
		var result = new byte[hex.Length / 2];
		for (var i = 0; i < hex.Length; i += 2)
		{
			result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
		}
		return result;
	}
}
