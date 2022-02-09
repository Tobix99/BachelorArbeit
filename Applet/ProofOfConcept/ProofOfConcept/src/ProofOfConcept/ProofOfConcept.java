package ProofOfConcept ;

import javacard.framework.*;
import javacardx.apdu.ExtendedLength;

import javacardx.crypto.Cipher;
import javacard.security.AESKey;
import javacard.security.KeyBuilder;
import javacard.security.RandomData;
import javacard.security.CryptoException;

public class ProofOfConcept extends Applet implements ExtendedLength 
{
	private RandomData randomData;
	private AESKey aesTempKey128;
	private AESKey aesPersistantKey128;
	private Cipher aesCipher;
	
	private boolean isHostVerified = false;
	
	private byte[] aesKeyInternalAuth = new byte[] { 0x12, 0x34, 0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x17 };
	private byte[] aesKeyExternalAuth = new byte[] { 0x12, 0x34, 0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18 };
	
	private byte[] IV = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x16, 0x00, 0x00 };
	
	byte[] tempByteArray; 
	byte[] challengeRandomNumber;
	
	public static void install(byte[] bArray, short bOffset, byte bLength) 
	{
		new ProofOfConcept().register(bArray, (short) (bOffset + 1), bArray[bOffset]);
	}
	
	private ProofOfConcept() {
		
		// Create all Objects for the Lifetime
		randomData = RandomData.getInstance(RandomData.ALG_SECURE_RANDOM);
		
		aesTempKey128 = (AESKey) KeyBuilder
				.buildKey(KeyBuilder.TYPE_AES_TRANSIENT_DESELECT, KeyBuilder.LENGTH_AES_128, false);
		
		
		// This Key is not really persistent, because the SIM doesn't store data over the session, normally you use KeyBuilder.TYPE_AES
		aesPersistantKey128 = (AESKey) KeyBuilder
				.buildKey(KeyBuilder.TYPE_AES_TRANSIENT_DESELECT, KeyBuilder.LENGTH_AES_128, false);
		
		aesCipher = Cipher.getInstance(Cipher.ALG_AES_BLOCK_128_CBC_NOPAD, false);
		
		
		// make transientArray
		challengeRandomNumber = JCSystem.makeTransientByteArray((short)16, JCSystem.CLEAR_ON_DESELECT);
		tempByteArray = JCSystem.makeTransientByteArray((short)1024, JCSystem.CLEAR_ON_DESELECT);

	}
	
	public boolean select(){
		isHostVerified = false;
		
		return true;
	}

	public void process(APDU apdu)
	{
		if (selectingApplet())
		{
			return;
		}

		byte[] buf = apdu.getBuffer();
		switch (buf[ISO7816.OFFSET_INS])
		{
		case (byte)0x01:
			// GetRandomNumber
			sendRandomBytesForExternalAuth(apdu, (short)4);
			break;
			
		case (byte)0x02:
			// internal Auth
			internalAuth(apdu);
			break;
			
		case (byte)0x03:
			//external auth
			externalAuth(apdu);
			break;
			
		case (byte)0x04:
			// key mgmt
			checkIfAuth();
			keyManagement(apdu);
			break;
			
		case (byte)0x06:
			//encrypt
			checkIfAuth();
			cryptData(apdu);
			break;
			
		default:
			ISOException.throwIt(ISO7816.SW_INS_NOT_SUPPORTED);
		}
	}
	
	private void checkIfAuth() {
		if (!isHostVerified) {
			ISOException.throwIt(ISO7816.SW_SECURITY_STATUS_NOT_SATISFIED);
		}
	}
	
	private void internalAuth(APDU apdu) {
		
		// get the Buffer Object
		byte[] buf = apdu.getBuffer();
		
		// get Bytes from the Buffer
		short incomingLength = apdu.setIncomingAndReceive();
		// check if 4 bytes (int32 Number)
		if (incomingLength != 16)
			ISOException.throwIt(ISO7816.SW_WRONG_LENGTH); // SW_Wrong_Lenght?
		
		
		short offsetCdata = apdu.getOffsetCdata();
		

		
		
		aesTempKey128.setKey(aesKeyInternalAuth, (short) 0);
		aesCipher.init(aesTempKey128, Cipher.MODE_ENCRYPT, IV, (short)0, (short)16);
		
		
	
		short le = aesCipher.doFinal(buf, // inBuffer
			offsetCdata, // inOffset
			incomingLength, // inLength
			tempByteArray, // outBuff
			(short)0); // outOffset

		
		Util.arrayCopy(tempByteArray, (short)0, buf, (short)0, le);
		
		apdu.setOutgoingAndSend((short)0, le);
		

	}


	private void externalAuth(APDU apdu) {
		
		byte[] buf = apdu.getBuffer();
		
		// decrypt incoming bytes
		short incomingLength = apdu.setIncomingAndReceive();
		short offsetCdata = apdu.getOffsetCdata();
		
		
		
		
		
		aesTempKey128.setKey(aesKeyExternalAuth, (short) 0);
		aesCipher.init(aesTempKey128, Cipher.MODE_DECRYPT, IV, (short)0, (short)16);

		
		try {
			aesCipher.doFinal(buf, // inBuffer
				offsetCdata, // inOffset
				incomingLength, // inLength
				tempByteArray, // outBuff
				(short)0); // outOffset

		}catch (CryptoException e){
			
		}
		
		// check if match data in challengeRandomNumber
		
		
		if(Util.arrayCompare(challengeRandomNumber, (short)0, tempByteArray, (short)0, (short)4) == 0){
			isHostVerified = true;
		}else{
			ISOException.throwIt(ISO7816.SW_DATA_INVALID);
		}
		
	}


	// send Random bytes, len in bytes
	private void sendRandomBytesForExternalAuth(APDU apdu, short len) {
		
		byte[] buf = apdu.getBuffer();
		
		// limit to 16 bytes
		if (len > (short) challengeRandomNumber.length) {

			// Throw an exception
			ISOException.throwIt(ISO7816.SW_WRONG_LENGTH);
		}

		

		randomData.generateData(challengeRandomNumber, (short) 0, len);
		
		
		// copy for send
		Util.arrayCopy(challengeRandomNumber, (short)0, buf, (short)0, (short)len);
		apdu.setOutgoingAndSend((short)0, (short)len);
	}


	private void keyManagement(APDU apdu) {
		
		byte[] buffer = apdu.getBuffer();
		
		// check p1 --> 0x01 --> create; 0x02 --> delete
		switch (buffer[ISO7816.OFFSET_P1])
		{
		case (byte)0x01:
			// create Key
			
			// create Random Data --> 128bit
			byte[] tempKey = new byte[16];
			randomData.generateData(tempKey, (short)0, (short)16);
			
			// import as key
			aesPersistantKey128.setKey(tempKey, (short)0);
			Util.arrayFillNonAtomic(tempKey, (short)0, (short)16, (byte)0x00);
			break;
			
		case (byte)0x02:
			// delete Key
			aesPersistantKey128.clearKey();
			break;
			
		default:
			ISOException.throwIt(ISO7816.SW_WRONG_P1P2);
			break;
		}
		
		
	}

	private void cryptData(APDU apdu) {
		

		
		byte[] buffer = apdu.getBuffer();
		short incomingLength = apdu.setIncomingAndReceive();
		short offsetCdata = apdu.getOffsetCdata();
		
		if(!aesPersistantKey128.isInitialized()) {
			ISOException.throwIt(ISO7816.SW_CONDITIONS_NOT_SATISFIED);
		}
		
		
		// check if data is 16 Byte block
		if ((incomingLength % 16) != 0) {
			ISOException.throwIt(ISO7816.SW_WRONG_LENGTH);
		}
		
		
		// check p1 --> 0x01 --> encrypt; 0x02 --> decrypt
		switch (buffer[ISO7816.OFFSET_P1])
		{
		case (byte)0x01:
			//encrypt
			aesCipher.init(aesPersistantKey128, Cipher.MODE_ENCRYPT);
			short len = aesCipher.doFinal(buffer, // inBuffer
				offsetCdata, // inOffset
				incomingLength, // inLength
				tempByteArray, // outBuff
				(short)0); // outOffset
			
			Util.arrayCopy(tempByteArray, (short)0, buffer, (short)0, (short)len);
			apdu.setOutgoingAndSend((short)0, (short)len);
			
			break;
			
			
		case (byte)0x02:
			//decrypt
			aesCipher.init(aesPersistantKey128, Cipher.MODE_DECRYPT);
			short len1 = aesCipher.doFinal(buffer, // inBuffer
				offsetCdata, // inOffset
				incomingLength, // inLength
				tempByteArray, // outBuff
				(short)0); // outOffset
			
			Util.arrayCopy(tempByteArray, (short)0, buffer, (short)0, (short)len1);
			apdu.setOutgoingAndSend((short)0, (short)len1);
				
			break;
		default:
			ISOException.throwIt(ISO7816.SW_WRONG_P1P2);
			break;
			
		}
		
		
	}

}
