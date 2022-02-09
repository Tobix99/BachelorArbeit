package de.tobiasheinze.helloworld ;

import javacard.framework.*;

public class helloworld extends Applet
{

	final static byte[] HelloWorldChar = {'H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd'};

	final static byte HELLO = (byte) 0x00;
	final static byte ECHO = (byte) 0x01;


	public static void install(byte[] bArray, short bOffset, byte bLength) 
	{
		new helloworld().register(bArray, (short) (bOffset + 1), bArray[bOffset]);
	}

	public void process(APDU apdu)
	{
		if (selectingApplet())
		{
			return;
		}

		byte[] buf = apdu.getBuffer();
		short le = apdu.setOutgoing();
		
		switch (buf[ISO7816.OFFSET_INS])
		{
			
		// return only "Hello world"
		case HELLO:
			
			apdu.setOutgoingLength(le);
			
			Util.arrayCopy(HelloWorldChar, (short)0, buf, (short)0, le);
			
			apdu.sendBytes((short)0, le);
			
			break;
			
		//	return data in DATA field
		case ECHO:
			
			apdu.setOutgoingLength(le);
			
			apdu.sendBytes(ISO7816.OFFSET_CDATA, le);
			
			break;
			
		default:
			ISOException.throwIt(ISO7816.SW_INS_NOT_SUPPORTED);
		}
	}

}
