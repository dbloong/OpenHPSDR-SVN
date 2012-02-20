///////////////////////////////////////////////////////////////////////
//  ErrorCodes.h
//		This file contains the definitions for all the error codes
// that can be generated by the CSound and CWave classes.
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
// PSK31Core Library for transmission and reception of PSK31 signals
//    using a PC soundcard  or .wav files.
//	               Copyright 2000, Moe Wheatley, AE4JY
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// http://www.gnu.org/copyleft/lesser.html
//////////////////////////////////////////////////////////////////////
//

#if !defined(ErrorCodes0A83B9C2_549A_11D2_A141_00A0C996E7F5__INCLUDED_)
#define ErrorCodes0A83B9C2_549A_11D2_A141_00A0C996E7F5__INCLUDED_

// generic error codes for all classes
#define NO_ERRORS 0		
#define MEMORY_ERROR 100

// input wave file error codes
#define WAVIN_ERR_OPEN 101			// can't open wave file for input
#define WAVIN_ERR_NOTWAVE 102		// file is not a RIFF wave type
#define WAVIN_ERR_INVALID 103		// Invalid wave file
#define WAVIN_ERR_NODATA 104		// no data in file
#define WAVIN_ERR_NOTSUPPORTED 105	// not a supported data type
#define WAVIN_ERR_READING 106		// Error reading data from file
#define WAVIN_ERR_NOTOPEN 107		// tried to read and file is not open

// output wave file error codes
#define WAVOUT_ERR_OPEN 108			// can't open wave file for output
#define WAVOUT_ERR_WRITING 109		// error writing to wave file
#define WAVOUT_ERR_NOTOPEN 110		// tried to write and file is not open

// Soundcard input error codes
#define SOUNDIN_ERR_NOTOPEN 111		// tried to read and soundcard is not open
#define SOUNDIN_ERR_OVERFLOW 112	// input buffers overflowed
#define SOUNDIN_ERR_TIMEOUT	113		// Timed out waiting for input buffers


// Soundcard output error codes
#define SOUNDOUT_ERR_NOTOPEN 114	// tried to write and soundcard is not open
#define SOUNDOUT_ERR_UNDERFLOW 115	// output buffers Underflowed
#define SOUNDOUT_ERR_TIMEOUT 116	// Timed out waiting for output buffers

// general multimedia error values as defined in mmsystem.h 
/* waveform audio error return values */
//#define WAVERR_BASE            32

//#define WAVERR_BADFORMAT      (WAVERR_BASE + 0)    /* unsupported wave format */
//#define WAVERR_STILLPLAYING   (WAVERR_BASE + 1)    /* still something playing */
//#define WAVERR_UNPREPARED     (WAVERR_BASE + 2)    /* header not prepared */
//#define WAVERR_SYNC           (WAVERR_BASE + 3)    /* device is synchronous */
//#define WAVERR_LASTERROR      (WAVERR_BASE + 3)    /* last error in range */

//#define MMSYSERR_BASE          0

//#define MMSYSERR_NOERROR      0                    // no error
//#define MMSYSERR_ERROR        (MMSYSERR_BASE + 1)  // unspecified error
//#define MMSYSERR_BADDEVICEID  (MMSYSERR_BASE + 2)  // device ID out of range
//#define MMSYSERR_NOTENABLED   (MMSYSERR_BASE + 3)  // driver failed enable
//#define MMSYSERR_ALLOCATED    (MMSYSERR_BASE + 4)  // device already allocated
//#define MMSYSERR_INVALHANDLE  (MMSYSERR_BASE + 5)  // device handle is invalid
//#define MMSYSERR_NODRIVER     (MMSYSERR_BASE + 6)  // no device driver present
//#define MMSYSERR_NOMEM        (MMSYSERR_BASE + 7)  // memory allocation error
//#define MMSYSERR_NOTSUPPORTED (MMSYSERR_BASE + 8)  // function isn't supported
//#define MMSYSERR_BADERRNUM    (MMSYSERR_BASE + 9)  // error value out of range
//#define MMSYSERR_INVALFLAG    (MMSYSERR_BASE + 10) // invalid flag passed
//#define MMSYSERR_INVALPARAM   (MMSYSERR_BASE + 11) // invalid parameter passed
//#define MMSYSERR_HANDLEBUSY   (MMSYSERR_BASE + 12) // handle being used
												     // simultaneously on another
												     // thread (eg callback)
//#define MMSYSERR_INVALIDALIAS (MMSYSERR_BASE + 13) // specified alias not found
//#define MMSYSERR_BADDB        (MMSYSERR_BASE + 14) // bad registry database
//#define MMSYSERR_KEYNOTFOUND  (MMSYSERR_BASE + 15) // registry key not found
//#define MMSYSERR_READERROR    (MMSYSERR_BASE + 16) // registry read error
//#define MMSYSERR_WRITEERROR   (MMSYSERR_BASE + 17) // registry write error
//#define MMSYSERR_DELETEERROR  (MMSYSERR_BASE + 18) // registry delete error
//#define MMSYSERR_VALNOTFOUND  (MMSYSERR_BASE + 19) // registry value not found
//#define MMSYSERR_NODRIVERCB   (MMSYSERR_BASE + 20) // driver does not call DriverCallback
//#define MMSYSERR_LASTERROR    (MMSYSERR_BASE + 20) // last error in range

#endif