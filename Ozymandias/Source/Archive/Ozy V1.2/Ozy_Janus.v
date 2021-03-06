//  V3 Feb 24, 2009 by Kirk Weedman KD7IRS
//
//  Copyright 2006,2007, 2008 Bill Tracey KD5TFD and Phil Harman VK6APH
//  Copyright 2009 2009 by Kirk Weedman KD7IRS
//
//  HPSDR - High Performance Software Defined Radio
//
//  Mercury, Penelope, Janus and Ozy interface.
//
//
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Full duplex Ozy interface for AK5394A  and TI TLV320AIC23B A/D converters
//
// The AK5394A and the TLV320 are both in slave mode
//
//
// Sends 192/96/48k 24 bit A/D data and 48k/16 bit mic/line data and receives 48k 16 bit
// L/R and I/Q data.
//
// A flag of 0x7F7F7F is sent prior to 5 bytes of control data then 3 bytes of
// left data, 3 bytes of right data and 2 bytes of mic/line data.  The control/data
// sequence is then repeated every 512 bytes.

// The format for data TO the PC is:-
//
//  <0x7F7F><0x7F,C0><C1,C2><C3,C4><Left><Left LSB,Right MSB><Right LSW><Mic data >... etc
//
// where Cn is a control byte - see protocol design document for full description.
//
// C0 obxxxx_xx00 PTT,dot and dash inactive
// C0 0bxxxx_xx01 PTT or dot key  activated 
// C0 0bxxxx_xx10 dash key activated
//
// where x = don't care
// etc....see protocol design document for full C&C format.

//               Left                             Right
//0-------------------------------------------------------------------------63
//0                 16       24                          35         51 52    BCLK counter (AD_state)
//<   Left data     ><  LSB  >       <   Right data      ><   LSB   >       - AK5394A
//<Mic MSB ><Mic LSB>                                                       - TLV320


// Sync and control bytes are sent as follows:
//   <0x7F7F> <0x7F,C0> <C1,C2> <C3,C4>


// The format for data FROM the PC is the same sync & control sequence followed by 48k/16 bit data:-
//
//   <0x7F7F><0x7F,C0><C1,C2><C3,C4>< Left data >< Right data >< I data >< Q data > etc...

// Control bytes are as follows:
//
// C0 = 0bxxxx_xxx0   MOX in PowerSDR inactive
// C0 = 0bxxxx_xxx1   MOX active
//
// AK5394A speed is set with the following command FROM the PC:
//
// When C0 = 0b0000_000x
// then
//      C1 = 0bxxxx_xx00 speed is 48k (default)
//      C1 = 0bxxxx_xx01 speed is 96k
//      C1 = 0bxxxx_xx10 speed is 192k
//
// where x = don't care
// etc....see protocol design document for full C&C format.

//
// A/D data is in 2's complement format.
// AK5394A is set to be 192/96/48kHz, 24 bit in I2S mode.
//
// TLV320 is set via I2C to be 48kHz, 16 bit and I2S mode as follows:
//
// TLV320  - 1A 1E 00 - Reset chip
//         - 1A 12 01 - set digital interface active
//         - 1A 0A 00 - turn D/A mute off
//         - 1A 08 15 - D/A on, mic input, mic 20dB boost
//         - 1A 0C 00 - All chip power on
//         - 1A 0E 02 - Slave, 16 bit, I2S
//         - 1A 10 00 - Clock/2, 48k, Normal mode
//
//         I2C address for TLV320 is 0x1A 

// Built with Quartus II v8.1 Build 163
//
// Change log:  Redesign of OzyJanus FPGA based upon only using IF_clk for logic - FEb 15, 2009
//
////////////////////////////////////////////////////////////


//////////////////////////////////////////////////////////////
//
//                      Pin Assignments
//
/////////////////////////////////////////////////////////////
//
//    AK5394A and LTV320AIC23B connections to OZY FPGA pins
//
//    AK_reset      - Atlas C2  - pin 149 - AK5394A reset
//    C48_clk       - Atlas C3  - pin 150 - 48MHz clock to PWM DACs
//    J_LR_data     - Atlas C4  - pin 151 - L/R audio to Janus in I2S format 
//    C5            - Atlas C5  - pin 152 - 12.288MHz clock from Janus
//    C6            - Atlas C6  - pin 160 - BCLK to Janus
//    C7            - Atlas C7  - pin 161 - LRCLK to Janus
//    C8            - Atlas C8  - pin 162 - CBCLK to Janus
//    C9            - Atlas C9  - pin 163 - CLRCLK to Janus
//    DOUT          - Atlas C10 - pin 164 - AK5394A
//    CDOUT         - Atlas C11 - pin 165 - Mic from TLV320 on Janus 
//    J_IQ_data     - Atlas C12 - pin 168 - I/Q audio (TLV320) to Janus in I2S format 
//    DFS0          - Atlas C13 - pin 169 - AK5394A speed setting
//    DFS1          - Atlas C14 - pin 170 - AK5394A speed setting
//    PTT_in        - Atlas C15 - pin 171 - PTT input from Janus
//                  - Atlas C16 - pin 173 - 10MHz reference 
//                  - Altas C17 - pin 175 - Master clock to Atlas for Janus etc 
//    P_IQ_data     - Atlas C19 - pin 179 - P_IQ_data (TLV320) to Penelope
//    P_IQ_sync     - Atlas C22 - pin 182 - P_IQ_sync from Penelope
//    M_LR_sync     - Atlas C23 - pin 185 - M_LR_sync from Mercury
//    M_LR_data     - Atlas C24 - pin 151 - M_LR_data to Mercury
//
//    A5            - Atlas A5  - pin 144 - Penelope NWire serial number, etc
//    A6            - Atlas A6	- pin 143 - Mercury NWire serial number, etc 
//    MDOUT	        - Atlas A10 - pin 138 - IQ from Mercury 
//    CDOUT_P       - Atlas A11 - pin 137 - Mic for TLV320 on Penelope
//    A12           = Atlas A12 - pin 135 - NWire spectrum data from Mercury 
//
//    DB9 - pin connections
//
//    dash          - Atlas       pin 97  - dash key    - DB9 pin 6
//    dot           - Atlas       pin 96  - PTT/dot key - DB9 pin 7
//
//
//    FX2 pin    to   FPGA pin connections
//
//    IF_clk        - pin 24
//    FX2_CLK       - pin 23
//    FX2_FD[0]     - pin 56
//    FX2_FD[1]     - pin 57
//    FX2_FD[2]     - pin 58
//    FX2_FD[3]     - pin 59
//    FX2_FD[4]     - pin 60
//    FX2_FD[5]     - pin 61
//    FX2_FD[6]     - pin 63
//    FX2_FD[7]     - pin 64
//    FX2_FD[8]     - pin 208
//    FX2_FD[9]     - pin 207
//    FX2_FD[10]    - pin 206
//    FX2_FD[11]    - pin 205
//    FX2_FD[12]    - pin 203
//    FX2_FD[13]    - pin 201
//    FX2_FD[14]    - pin 200
//    FX2_FD[15]    - pin 199
//    FLAGA         - pin 198
//    FLAGB         - pin 197
//    FLAGC         - pin 5
//    SLOE          - pin 13
//    FIFO_ADR[0]   - pin 11
//    FIFO_ADR[1]   - pin 10
//    PKEND         - pin 8
//    SLRD          - pin 30
//    SLWR          - pin 31
//
//
//   General FPGA pins
//
//    DEBUG_LED0    - pin 4
//    DEBUG_LED1    - pin 33
//    DEBUG_LED2    - pin 34
//    DEBUG_LED3    - pin 108
//    FPGA_GPIO1    - pin 67
//    FPGA_GPIO2    - pin 68
//    FPGA_GPIO3    - pin 69
//    FPGA_GPIO4    - pin 70
//    FPGA_GPIO5    - pin 72
//    FPGA_GPIO6    - pin 74
//    FPGA_GPIO7    - pin 75
//    FPGA_GPIO8    - pin 76
//    FPGA_GPIO9    - pin 77
//    FPGA_GPIO10   - pin 80
//    FPGA_GPIO11   - pin 81
//    FPGA_GPIO12   - pin 82
//    FPGA_GPIO13   - pin 84
//    FPGA_GPIO14   - pin 86
//    FPGA_GPIO15   - pin 87
//    FPGA_GPIO16   - pin 88
//    FPGA_GPIO17   - pin 89
//
//
////////////////////////////////////////////////////////////////////////////////////
`timescale 1 ns/100 ps

module Ozy_Janus(
        IF_clk, FX2_FD, FLAGA, FLAGB, FLAGC, SLWR, SLRD, SLOE, PKEND, FIFO_ADR, DOUT,
        A5, A6, A12, C4, C5, C6, C7, C8, C9, C12, C13, C14, C19, C21, C22, C23, C24,
        CDOUT, CDOUT_P, PTT_in, AK_reset,DEBUG_LED0,
        DEBUG_LED1, DEBUG_LED2,DEBUG_LED3, C48_clk, CC, MDOUT, /*FX2_CLK,*/
        SPI_SCK, SPI_SI, SPI_SO, SPI_CS, GPIO_OUT, GPIO_IN, GPIO_nIOE,
        /*FX2_PE0,*/ FX2_PE1, /*FX2_PE2, FX2_PE3,*/ SDOBACK /*, TDO, TCK, TMS */ );

parameter M_TPD   = 4;
parameter IF_TPD  = 2;

localparam Ozy_serialno = 8'd13;	// Serial number of this version

localparam RX_FIFO_SZ  = 2048; // 16 by 2048 deep RX FIFO
localparam TX_FIFO_SZ  = 4096; // 16 by 4096 deep TX FIFO
localparam SP_FIFO_SZ  = 1024; // 16 by 1024 deep SP FIFO

input  wire         IF_clk;          // FX2 IFCLOCK - 48MHz
input  wire         DOUT;           // Data from AK5394A
input  wire         MDOUT;          // I&Q data from Mercury 
inout  wire  [15:0] FX2_FD;         // bidirectional FIFO data to/from the FX2

input  wire         FLAGA;
input  wire         FLAGB;
input  wire         FLAGC;
output wire         SLWR;           // FX2 write - active low
output wire         SLRD;           // FX2 read - active low
output wire         SLOE;           // FX2 data bus enable - active low

output wire         PKEND;
output wire   [1:0] FIFO_ADR;       // FX2 register address
output wire         DEBUG_LED0;     // LEDs on OZY board
output wire         DEBUG_LED1;
output wire         DEBUG_LED2;
output wire         DEBUG_LED3;
input  wire         CDOUT;          // A/D data from TLV320AIC23B
input  wire         PTT_in;         // PTT active high

input  wire         A5;             // NWire data from Penelope (serial number and ALC data)
input  wire         A6;             // NWire data from Mercury (serial number)
input  wire         A12;            // NWire spectrum data from Mercury

output wire         C4;             // J_LR_data - Left & Right audio data in I2S format to Janus
input  wire         C5;             // 12.288Mhz from Janus
output wire         C6;             // BCLK to Janus
output wire         C7;             // LRCLK to Janus
output wire         C8;             // CBCLK to Janus
output wire         C9;             // CLRCLK to Janus
output wire         C12;            // J_IQ_data - Rx data to TLV320AIC23B to Janus
output wire         C13;            // DFS0
output wire         C14;            // DFS1
output wire         C19;            // P_IQ_data - Rx data to TLV320AIC23B on Penelope
output wire         C21;            // Spectrum data Trigger signal to Mercury
input  wire         C22;            // P_IQ_sync from Penelope
input  wire         C23;            // M_LR_sync from Mercury
output wire         C24;            // M_LR_data - Left & Right audio data in NWire format to Mercury


output wire         AK_reset;       // reset for AK5394A - active low
output wire         C48_clk;        // 48MHz clock to Janus for PWM DACs 
output wire         CC;             // Command and Control data to Atlas bus 
input  wire         CDOUT_P;        // Mic data from Penelope

// interface lines for GPIO control 
//input  wire         FX2_CLK;        // master system clock from FX2 
input  wire         SPI_SCK;        // SPI SCK from FX2
input  wire         SPI_SI;         // SPI serial in from FX2
inout  wire         SPI_SO;         // SPI serial out to FX2
input  wire         SPI_CS;         // FPGA chip select from FX2
output wire  [15:0] GPIO_OUT;       // OZY GPIO lines
input  wire   [7:0] GPIO_IN;        // OZY GPIO lines
output wire 				GPIO_nIOE;      // enable GPIO driver chips 

// interface pins for JTAG programming via Atlas bus
//input  wire         FX2_PE0;        // Port E on FX2
output wire         FX2_PE1;
//input  wire         FX2_PE2;
//input  wire         FX2_PE3;
input  wire         SDOBACK;        // A25 on Atlas
//output wire         TDO;            // A27 on Atlas 
//output wire         TCK;            // A24 on Atlas
//output wire         TMS;            // A23 on Atlas

// internal signals
wire          dot_n;    // CW dot key, active low
wire          dash_n;   // CW dash key, active low

assign dot_n  = GPIO_IN[6];   // alias dot and dash to appropriate GPIO lines 
assign dash_n = GPIO_IN[5]; 

assign GPIO_nIOE = 1'b0; 

// link JTAG pins through
//assign TMS = FX2_PE3;
//assign TCK = FX2_PE2;
//assign TDO = FX2_PE0;  // TDO on our slot ties to TDI on next slot  
assign FX2_PE1 = SDOBACK;

///////////////////////////////////////////////////////////////
//
//              GPIO
//
///////////////////////////////////////////////////////////////
// instantiate gpio control block 
gpio_control gpio_controlSDR(
                .SCK(SPI_SCK), 
                .SI(SPI_SI), 
                .SO(SPI_SO), 
                .CS(SPI_CS), 
                .GPIO({GPIO_IN,GPIO_OUT})
							 );
///////////////////////////////////////////////////////////////
//
//              3X clock multiplier  48MHz -> 144Mhz
//
///////////////////////////////////////////////////////////////
wire C144_clk;
wire C144_clk_locked;
reg  IF_rst, cmult_rst;

clkmult3 cm3 (.areset(cmult_rst), .inclk0(IF_clk),.c0(C144_clk), .locked(C144_clk_locked));


//////////////////////////////////////////////////////////////
//
// 48MHz clock to Janus
//
/////////////////////////////////////////////////////////////

reg   [1:0] conf;

// **** only send 48MHz clock to Atlas bus if Mercury not fitted to see effect on spurs
// conf[1] = 1 if Mercury selected for Rx output
assign C48_clk = conf[1] ? 1'b0 : IF_clk; 	// 12MHz clock to PWM DAC on Janus only if Mercury not selected

//////////////////////////////////////////////////////////////
//
// cmult_rst, C144_rst, IF_rst, AK_reset, C12_rst
//
/////////////////////////////////////////////////////////////

/*
        Reset AL5394A at power on and force into 48kHz sampling rate.
        Hold the A/D chip in reset until 2^28 CLK_MCLK have passed - about 3 seconds. This
        is to allow the AK4593A to calibrate correctly.
*/

reg [28:0] IF_count;
wire       C144_rst;

always @ (posedge IF_clk)
begin: IF_RST
  reg i0;
  
  if (!IF_count[28])
    IF_count <= IF_count + 1'b1; // count up from 0 (powerup reset value) till IF_count[28] is set

  cmult_rst <= (IF_count[28:10] == 0) ? 1'b1 : 1'b0; // This will be a global power up reset for the IF_clk domain

  {IF_rst, i0} <= {i0, !C144_clk_locked}; // clock multiplier needs to be locked before any code runs
end

reg   C12_rst;
wire  C12_clk;  // 12.288 Mhz from Janus

always @ (posedge C12_clk)
begin: C12_RST
  reg c0;
  
  {C12_rst, c0} <= {c0, !C144_clk_locked}; // clock multiplier needs to be locked before any code runs
end

assign C144_rst  = !C144_clk_locked;

assign AK_reset = IF_count[28];  // AK_reset (active low) is long after powerup

///////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Create BCLK/LRCLK and CBCLK/CLRCLK for Janus
//
// make sure CBCLK/CLRCLK and BCLK/LRCLK stay synced when SPEED changes
//
///////////////////////////////////////////////////////////////////////////////////////////////////////
reg       C12_cgen_rst;
reg [1:0] C12_speed;
reg       C12_DFS_ack;
reg       IF_DFS0;       // used to set AK5394A speed
reg       IF_DFS1;       // ditto 
reg [1:0] IF_last_DFS;
reg       IF_DFS_rdy;
reg       IF_DFS_ack;

localparam SPEED_48K = 2'b00;

assign C13 = IF_DFS0;
assign C14 = IF_DFS1;

assign C12_clk = C5;

always @(posedge IF_clk)
begin: DFS_HANDSHAKE // signals are going from fast clock domain to a slower one
  reg ack0;
  
  if (IF_rst)
    IF_last_DFS <= #IF_TPD 2'b00;
  else
    IF_last_DFS <= #IF_TPD {IF_DFS1, IF_DFS0};

  if (IF_rst)
    IF_DFS_rdy <= #IF_TPD 1'b0;
  else if (IF_last_DFS != {IF_DFS1, IF_DFS0})
    IF_DFS_rdy <= #IF_TPD 1'b1;  // set rdy true since we just changed value
  else if (IF_DFS_ack)
    IF_DFS_rdy <= #IF_TPD 1'b0;  // clear rdy when we get an ack from C12_clk domain

  if (IF_rst)
    {IF_DFS_ack, ack0} <= #IF_TPD 2'b00;
  else
    {IF_DFS_ack, ack0}  <= #IF_TPD {ack0, C12_DFS_ack};
end

always @(posedge C12_clk)
begin: C12_SPEED
  reg [1:0] df0, df1;
  reg       rdy2, rdy1, rdy0;
  
  if (C12_rst)
    {df1,df0} <= {2'b00,2'b00};
  else
    {df1,df0} <= {df0,{IF_DFS1,IF_DFS0}}; // get IF_DFS1, IF_DFS0 into C12_clk domain

  if (C12_rst)
    {C12_DFS_ack, rdy2, rdy1, rdy0} <= 4'b0000;
  else // note: ack goes true once C12_speed is latched
    {C12_DFS_ack, rdy2, rdy1, rdy0}<= {rdy2, rdy1, rdy0, IF_DFS_rdy}; // get IF_DFS_rdy into C12_clk domain

  if (C12_rst)
    C12_speed <= 2'b00;
  else if (rdy2)
    C12_speed <= df1;

  if (C12_rst)
    C12_cgen_rst <= 1'b1;
  else if (rdy2)
    C12_cgen_rst <= 1'b1; // resynchronize all the BCLK/LRCLK/CBCLK/CLRCLK for the new speed
  else
    C12_cgen_rst <= 1'b0;
end

wire C12_LRCLK, C12_cbrise, C12_cbfall, C12_rise, C12_fall;
wire C12_CBCLK, C12_CLRCLK;

clk_lrclk_gen #(.CLK_FREQ(12288000))
         lrgen (.reset(C12_cgen_rst), .CLK_IN(C12_clk), .Brise(C12_cbrise), .Bfall(C12_cbfall),
                .BCLK(),  .LRCLK(C12_LRCLK),  .Speed(C12_speed));
clk_lrclk_gen #(.CLK_FREQ(12288000))
        clrgen (.reset(C12_cgen_rst), .CLK_IN(C12_clk), .Brise(C12_rise), .Bfall(C12_fall),
                .BCLK(C12_CBCLK), .LRCLK(C12_CLRCLK), .Speed(SPEED_48K));

assign C6 = C12_clk;
assign C7 = C12_LRCLK;
assign C8 = C12_CBCLK;
assign C9 = C12_CLRCLK;

///////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  Recieve MDOUT/DOUT and CDOUT/CDOUT_P data to put in TX FIFO
//
///////////////////////////////////////////////////////////////////////////////////////////////////////
wire [15:0] IF_P_mic_Data;
wire        IF_P_mic_Data_rdy;
wire [15:0] IF_J_mic_Data;
wire        IF_J_mic_Data_rdy;
wire [47:0] IF_M_IQ_Data;
wire        IF_M_IQ_Data_rdy;
wire [47:0] IF_J_IQ_Data;
wire        IF_J_IQ_Data_rdy;
wire [63:0] IF_Audio_Data;
reg         IF_Audio_Data_rdy;
reg         mic;
reg   [3:0] IF_clock_s;
reg  [63:0] IF_Tx_IQ_mic_data;
reg         IF_Tx_IQ_mic_rdy;
wire [47:0] IF_IQ_Data;
wire [15:0] IF_mic_Data;

// IF_M_IQ_Data_rdy : IF_P_mic_Data_rdy occur at different times so sync TX data
always @(posedge IF_clk)
begin
  if (IF_Audio_Data_rdy)
    IF_Tx_IQ_mic_data <= #IF_TPD IF_Audio_Data;

  if (IF_rst)
    IF_Tx_IQ_mic_rdy <= #IF_TPD 1'b0;
  else
    IF_Tx_IQ_mic_rdy <= #IF_TPD IF_Audio_Data_rdy;
end

always @*
begin
  if (conf[1])
    IF_Audio_Data_rdy = IF_M_IQ_Data_rdy;
  else if (conf[0])
    IF_Audio_Data_rdy = IF_P_mic_Data_rdy;
  else
    IF_Audio_Data_rdy = IF_J_IQ_Data_rdy;
end

assign IF_IQ_Data    = (conf[1] ? IF_M_IQ_Data  : IF_J_IQ_Data);
                    
assign IF_mic_Data   = (mic ? IF_P_mic_Data : IF_J_mic_Data);

assign IF_Audio_Data = {IF_IQ_Data, IF_mic_Data};

NWire_rcv #(.DATA_BITS(16), .ICLK_FREQ(144000000), .XCLK_FREQ(48000000), .SLOWEST_FREQ(20000))
    P_MIC (.irst(C144_rst), .iclk(C144_clk), .xrst(IF_rst), .xclk(IF_clk),
           .xrcv_rdy(IF_P_mic_Data_rdy), .xrcv_ack(IF_Tx_IQ_mic_rdy),
           .xrcv_data(IF_P_mic_Data), .din(CDOUT_P) );

// Get I2S CDOUT mic data from Janus.  NOTE: only 16 bits used
I2S_rcv #(32,2,1) // WARNING: values 2,1 may need adjusting for best capture of data
    J_MIC (.xrst(IF_rst), .xclk(IF_clk), .BCLK(C12_CBCLK), .LRCLK(C12_CLRCLK), .din(CDOUT),
           .xData(IF_J_mic_Data), .xData_rdy(IF_J_mic_Data_rdy));


NWire_rcv #(.DATA_BITS(48), .ICLK_FREQ(144000000), .XCLK_FREQ(48000000), .SLOWEST_FREQ(20000))
    M_IQ (.irst(C144_rst), .iclk(C144_clk), .xrst(IF_rst), .xclk(IF_clk),
           .xrcv_rdy(IF_M_IQ_Data_rdy), .xrcv_ack(IF_Tx_IQ_mic_rdy),
           .xrcv_data(IF_M_IQ_Data), .din(MDOUT) );

// Get I2S DOUT IQ data from Janus
I2S_rcv #(48,2,1) // WARNING: values 2,1 may need adjusting for best capture of data
    J_IQ (.xrst(IF_rst), .xclk(IF_clk), .BCLK(C12_clk), .LRCLK(C12_LRCLK), .din(DOUT),
          .xData(IF_J_IQ_Data), .xData_rdy(IF_J_IQ_Data_rdy));

///////////////////////////////////////////////////////////////
//
//     Tx_fifo Control - creates Tx_fifo_wdata and Tx_fifo_wreq signals
//
//////////////////////////////////////////////////////////////
localparam RFSZ = clogb2(RX_FIFO_SZ-1);  // number of bits needed to hold 0 - (RX_FIFO_SZ-1)
localparam TFSZ = clogb2(TX_FIFO_SZ-1);  // number of bits needed to hold 0 - (TX_FIFO_SZ-1)
localparam SFSZ = clogb2(SP_FIFO_SZ-1);  // number of bits needed to hold 0 - (SP_FIFO_SZ-1)

wire     [15:0] Tx_fifo_wdata;   // AK5394A A/D uses this to send its data to Tx FIFO
wire            Tx_fifo_wreq;    // set when we want to send data to the Tx FIFO
wire            Tx_fifo_full;
wire [TFSZ-1:0] Tx_fifo_used;
wire     [15:0] Tx_fifo_rdata;
wire            Tx_fifo_rreq;
wire            Tx_fifo_empty;

wire [RFSZ-1:0] Rx_fifo_used;    // read side count
wire            Rx_fifo_full;

wire            clean_dash;      // debounced dash
wire            clean_dot;       // debounced dot
wire            clean_PTT_in;    // debounced button
reg       [7:0] Penny_serialno;
reg       [7:0] Merc_serialno;
reg      [11:0] Penny_ALC;

reg             ADC_OVERLOAD;
wire   [RFSZ:0] RX_USED;
wire            Tx_fifo_clr;

assign RX_USED = {Rx_fifo_full,Rx_fifo_used};

Tx_fifo_ctrl #(RX_FIFO_SZ, TX_FIFO_SZ) TXFC 
           (IF_rst, IF_clk, Tx_fifo_wdata, Tx_fifo_wreq, Tx_fifo_full, Tx_fifo_used,
            Tx_fifo_clr, Rx_fifo_used, Rx_fifo_full, IF_Tx_IQ_mic_rdy, IF_Tx_IQ_mic_data,
            clean_dash, clean_dot, clean_PTT_in, ADC_OVERLOAD,
            Penny_serialno, Merc_serialno, Ozy_serialno, Penny_ALC);

///////////////////////////////////////////////////////////////
//
//     Tx_fifo (4096 words) single clock FIFO  - Altera Megafunction
//
//////////////////////////////////////////////////////////////


// NOTE: Reset Tx_fifo when {IF_DFS1,IF_DFS0} changes!!!???
Tx_fifo TXF (.sclr(IF_rst || Tx_fifo_clr), .clock (IF_clk), .full(Tx_fifo_full), 
             .empty(Tx_fifo_empty), .usedw(Tx_fifo_used),
             .wrreq (Tx_fifo_wreq), .data (Tx_fifo_wdata),
             .rdreq (Tx_fifo_rreq), .q    (Tx_fifo_rdata) );


/////////////////////////////////////////////////////////////
//
//   Rx_fifo  (2048 words) single clock FIFO - Altera Megafunction
//
/////////////////////////////////////////////////////////////

/*
        The FIFO is 2048 words long.
        NB: The output flags are only valid after a read/write clock has taken place
*/

wire [15:0] Rx_fifo_rdata;
reg         Rx_fifo_rreq;    // controls reading of fifo

wire [15:0] Rx_fifo_wdata;
reg         Rx_fifo_wreq;

// NOTE: Reset Rx_fifo when {IF_DFS1,IF_DFS0} changes!!!???
Rx_fifo RXF (.sclr(IF_rst), .clock (IF_clk), .full(Rx_fifo_full), .usedw(Rx_fifo_used),
             .wrreq (Rx_fifo_wreq), .data (Rx_fifo_wdata), 
             .rdreq (Rx_fifo_rreq), .q    (Rx_fifo_rdata) );

/////////////////////////////////////////////////////////////
//
//   SP_fifo  (1024 words) single clock FIFO - Altera Megafunction
//
/////////////////////////////////////////////////////////////

/*
        The spectrum data FIFO is 16 by 1024 words long.
        NB: The output flags are only valid after a read/write clock has taken place
*/

wire     [15:0] sp_fifo_rdata;
wire            sp_fifo_rreq;    // controls reading of fifo

wire     [15:0] sp_fifo_wdata;
reg             sp_fifo_wreq;

wire            sp_fifo_full;
wire            sp_fifo_empty;
wire [SFSZ-1:0] sp_fifo_used;    // read side count

SP_fifo SPF (.sclr(IF_rst), .clock (IF_clk), .full(sp_fifo_full), .usedw(sp_fifo_used),
             .wrreq (sp_fifo_wreq), .data (sp_fifo_wdata), .rdreq (sp_fifo_rreq),
             .q(sp_fifo_rdata) );

///////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  Receive Spectrum Data from Mercury
//
///////////////////////////////////////////////////////////////////////////////////////////////////////
wire spd_rdy;
reg  spd_ack;
wire trigger;

always @(posedge IF_clk)
begin
  if (IF_rst)
    spd_ack <= #IF_TPD 1'b0;
  else
    spd_ack <= spd_rdy; // one IF_clk delay

  if (IF_rst)
    sp_fifo_wreq <= #IF_TPD 1'b0;
  else
    sp_fifo_wreq <= spd_rdy & !spd_ack;
end

NWire_rcv #(.DATA_BITS(16), .ICLK_FREQ(144000000), .XCLK_FREQ(48000000), .SLOWEST_FREQ(80000))
       SPD (.irst(C144_rst), .iclk(C144_clk), .xrst(IF_rst), .xclk(IF_clk),
            .xrcv_rdy(spd_rdy), .xrcv_ack(spd_ack), .xrcv_data(sp_fifo_wdata), .din(A12) );

assign C21 = trigger;

sp_rcv_ctrl SPC (.rst(IF_rst), .clk(IF_clk), .trigger(trigger), .fifo_wreq(sp_fifo_wreq),
                 .flag(FLAGB));

//////////////////////////////////////////////////////////////
//
//   Interface to FX2 USB interface and FIFOs
//
//////////////////////////////////////////////////////////////
wire Rx_fifo_drdy;

async_usb #(3, RX_FIFO_SZ, 64, TX_FIFO_SZ, 256, SP_FIFO_SZ, 64)
          usb1 (IF_clk, IF_rst, FX2_FD, FLAGA, FLAGB, FLAGC, SLWR, SLRD, SLOE, PKEND, FIFO_ADR,
          Rx_fifo_drdy, Rx_fifo_wdata, Rx_fifo_used, Rx_fifo_full,
          Tx_fifo_rreq, Tx_fifo_rdata, Tx_fifo_used, Tx_fifo_full,
          sp_fifo_rreq, sp_fifo_rdata, sp_fifo_used, sp_fifo_full);
/*
sync_usb  #(3, RX_FIFO_SZ, 64, TX_FIFO_SZ, 256) //RX FIFO = 2048, TX FIFO = 4096
    usb1 (IF_clk, IF_rst, FX2_FD, FLAGA, FLAGC, SLWR, SLRD, SLOE, PKEND, FIFO_ADR,
          Rx_fifo_wdata, Rx_fifo_drdy, Rx_fifo_used, Rx_fifo_full,
          Tx_fifo_rreq, Tx_fifo_full, Tx_fifo_used, Tx_fifo_rdata, Tx_fifo_empty);
*/

//////////////////////////////////////////////////////////////
//
//   Sync and  C&C  Detector
//
//////////////////////////////////////////////////////////////

/*

  Read the value of Rx_fifo_wdata whenever Rx_fifo_wreq is set.
  Look for sync and if found decode the C&C data.
  Then send subsequent data to Rx FIF0 until end of frame.
	
*/

reg   [2:0] SYNC_state;
reg   [2:0] SYNC_state_next;
reg   [7:0] SYNC_frame_cnt; // 256-4 words = 252 words
reg   [7:0] Rx_control_0;   // control C0 from PC
reg   [7:0] Rx_control_1;   // control C1 from PC
reg   [7:0] Rx_control_2;   // control C2 from PC
reg   [7:0] Rx_control_3;   // control C3 from PC
reg   [7:0] Rx_control_4;   // control C4 from PC

localparam SYNC_IDLE   = 0,
           SYNC_START  = 1,
           SYNC_RX_1_2 = 2,
           SYNC_RX_3_4 = 3,
           SYNC_RX     = 4,
           SYNC_FINISH = 5;

always @ (posedge IF_clk)
begin
  if (IF_rst)
    SYNC_state <= #IF_TPD SYNC_IDLE;
  else
    SYNC_state <= #IF_TPD SYNC_state_next;

  if (Rx_fifo_drdy && (SYNC_state == SYNC_START) && (Rx_fifo_wdata[15:8] == 8'h7F))
    Rx_control_0  <= #IF_TPD Rx_fifo_wdata[7:0];

  if (Rx_fifo_drdy && (SYNC_state == SYNC_RX_1_2))
  begin
    Rx_control_1  <= #IF_TPD Rx_fifo_wdata[15:8];
    Rx_control_2  <= #IF_TPD Rx_fifo_wdata[7:0];
  end

  if (Rx_fifo_drdy && (SYNC_state == SYNC_RX_3_4))
  begin
    Rx_control_3  <= #IF_TPD Rx_fifo_wdata[15:8];
    Rx_control_4  <= #IF_TPD Rx_fifo_wdata[7:0];
  end

  if (SYNC_state == SYNC_START)
    SYNC_frame_cnt <= 0;					    // reset sync counter
  else if (Rx_fifo_drdy && (SYNC_state == SYNC_FINISH))
    SYNC_frame_cnt <= SYNC_frame_cnt + 1'b1;
end

always @*
begin
  case (SYNC_state)
    // state SYNC_IDLE  - loop until we find start of sync sequence
    SYNC_IDLE:
    begin
      Rx_fifo_wreq  = 1'b0;             // Note: Sync bytes not saved in Rx_fifo

      if (IF_rst || !Rx_fifo_drdy)              
        SYNC_state_next = SYNC_IDLE;    // wait till we get data from PC
      else if (Rx_fifo_wdata == 16'h7F7F)
        SYNC_state_next = SYNC_START;   // possible start of sync
      else
        SYNC_state_next = SYNC_IDLE;
    end	

    // check for 0x7F  sync character & get Rx control_0 
    SYNC_START:
    begin
      Rx_fifo_wreq  = 1'b0;             // Note: Sync bytes not saved in Rx_fifo

      if (!Rx_fifo_drdy)              
        SYNC_state_next = SYNC_START;   // wait till we get data from PC
      else if (Rx_fifo_wdata[15:8] == 8'h7F)
        SYNC_state_next = SYNC_RX_1_2;  // have sync so continue
      else
        SYNC_state_next = SYNC_IDLE;    // start searching for sync sequence again
    end

    
    SYNC_RX_1_2:                        // save Rx control 1 & 2
    begin
      Rx_fifo_wreq  = 1'b0;             // Note: Rx control 1 & 2 not saved in Rx_fifo

      if (!Rx_fifo_drdy)              
        SYNC_state_next = SYNC_RX_1_2;  // wait till we get data from PC
      else
        SYNC_state_next = SYNC_RX_3_4;
    end

    SYNC_RX_3_4:                        // save Rx control 3 & 4
    begin
      Rx_fifo_wreq  = 1'b0;             // Note: Rx control 3 & 4 not saved in Rx_fifo

      if (!Rx_fifo_drdy)              
        SYNC_state_next = SYNC_RX_3_4;  // wait till we get data from PC
      else
        SYNC_state_next = SYNC_RX;
    end

    SYNC_RX:                            // save Rx control bytes during this state
    begin
      Rx_fifo_wreq  = 1'b0;

      SYNC_state_next = SYNC_FINISH;
    end

    // Remainder of data goes to Rx_fifo, re-start looking
    // for a new SYNC at end of this frame.
    SYNC_FINISH:
    begin
      Rx_fifo_wreq  = Rx_fifo_drdy;

      if (SYNC_frame_cnt == ((512-8)/2)) // frame ended, go get sync again
        SYNC_state_next = SYNC_IDLE;
      else
        SYNC_state_next = SYNC_FINISH;
    end

    default:
    begin
      Rx_fifo_wreq  = 1'b0;

      SYNC_state_next = SYNC_IDLE;
    end
	endcase
end


//////////////////////////////////////////////////////////////
//
//              Decode Command & Control data
//
//////////////////////////////////////////////////////////////

/*
	Decode Rx_control_0....Rx_control_4.

	Decode frequency (for Mercury and Penelope), PTT and Speed 

	The current frequency is set by the PC by decoding 
	Rx_control_1... Rx_control_4 when Rx_control_0[7:1] = 7'b0000_001
		
      The speed the AK5394A runs at, either 192k, 96k or 48k is set by
      the PC by decoding Rx_control_1 when Rx_control_0[7:1] are all zero. Rx_control_1
      decodes as follows:

      Rx_control_1 = 8'bxxxx_xx00  - 48kHz
      Rx_control_1 = 8'bxxxx_xx01  - 96kHz
      Rx_control_1 = 8'bxxxx_xx10  - 192kHz

	Decode PTT from PowerSDR. Held in Rx_control_0[0] as follows
	
	0 = PTT inactive
	1 = PTT active
	
	Decode clock sources, when Rx_control_0[7:1] = 0,  Rx_control_1[4:2] indicates the following
	
	x00  = 10MHz reference from Atlas bus ie Gibraltar
	x01  = 10MHz reference from Penelope
	x10  = 10MHz reference from Mercury
	0xx  = 12.288MHz source from Penelope 
	1xx  = 12.288MHz source from Mercury 
	
	Decode configuration, when Rx_control_0[7:1] = 0, Rx_control_1[6:5] indicates the following
	
	00 = No Tx Rx boards
	01 = Penelope fitted
	10 = Mercury fitted
	11 = Both Penelope and Mercury fitted
	
	Decode microphone source, when Rx_control_0[7:1] = 0, Rx_control_1[7] indicates the following
	
	0 = microphone source is Janus
	1 = microphone source is Penelope
	
	Decode Attenuator settings on Alex, when Rx_control_0[7:1] = 0, Rx_control_3[1:0] indicates the following 
	
	00 = 0dB
	01 = 10dB
	10 = 20dB
	11 = 30dB
	
	Decode ADC settings on Mercury, when Rx_control_0[7:1] = 0, Rx_control_3[4:2] indicates the following
	
	000 = Random, Dither, gain off
	1xx = Random ON
	x1x = Dither ON
	xx1 = Gain ON 
	
	Decode Rx relay settigs on Alex, when Rx_control_0[7:1] = 0, Rx_control_3[6:5] indicates the following
	
	00 = None
	01 = Rx 1
	10 = Rx 2
	11 = Transverter
	
	Decode Tx relay settigs on Alex, when Rx_control_0[7:1] = 0, Rx_control_4[1:0] indicates the following
	
	00 = Tx 1
	01 = Tx 2
	10 = Tx 3
	
	Decode Rx_1_out relay settigs on Alex, when Rx_control_0[7:1] = 0, Rx_control_3[7] indicates the following

	1 = Rx_1_out on 
	
*/

wire        PTT_out;
reg   [6:0] OC;         // open collectors on Penelope
reg         mode;       // normal or Class E PA operation 
reg         RAND;       // when set randomizer in ADC on Mercury on
reg         DITHER;     // when set dither in ADC on Mercury on
reg         PGA;        // when set gain in ADC on Mercury set to 3dB else 0dB
reg   [1:0] ATTEN;      // decode attenuator setting on Alex
reg   [1:0] TX_relay;   // Tx relay setting on Alex
reg         Rout;       // Rx1 out on Alex
reg   [1:0] RX_relay;   // Rx relay setting on Alex 
reg  [31:0] frequency;

always @ (posedge IF_clk)
begin 
  if (IF_rst)
  begin // set up default values - 0 for now
    // RX_CONTROL_1
    {IF_DFS1, IF_DFS0} <= 2'b00;   // decode speed 
    IF_clock_s         <= 4'b0100; // decode clock source - default Mercury
    conf               <= 2'b00;   // decode configuration
    mic                <= 1'b0;    // decode microphone source
    // RX_CONTROL_2
    mode               <= 1'b0;    // decode mode, normal or Class E PA
    OC                 <= 7'b0;    // decode open collectors on Penelope
    // RX_CONTROL_3
    ATTEN              <= 2'b0;    // decode Alex attenuator setting 
    PGA                <= 1'b0;    // decode ADC gain high or low
    DITHER             <= 1'b0;    // decode dither on or off
    RAND               <= 1'b0;    // decode randomizer on or off
    RX_relay           <= 2'b0;    // decode Alex Rx relays
    Rout               <= 1'b0;    // decode Alex Rx_1_out relay
    // RX_CONTROL_4
    TX_relay           <= 2'b0;    // decode Alex Tx Relays
    frequency          <= 32'd0;
  end
  else if (SYNC_state == SYNC_RX) // all Rx_control bytes are ready to be saved
  begin 								// Need to ensure that C&C data is stable 
    if (Rx_control_0[7:1] == 7'b0000_000)
    begin
      // RX_CONTROL_1
      {IF_DFS1, IF_DFS0}  <= Rx_control_1[1:0]; // decode speed 
      IF_clock_s[2:0]     <= Rx_control_1[4:2]; // decode clock source
      conf                <= Rx_control_1[6:5]; // decode configuration
      mic                 <= Rx_control_1[7];   // decode microphone source
      // RX_CONTROL_2
      mode                <= Rx_control_2[0];   // decode mode, normal or Class E PA
      OC                  <= Rx_control_2[7:1]; // decode open collectors on Penelope
      // RX_CONTROL_3
      ATTEN               <= Rx_control_3[1:0]; // decode Alex attenuator setting 
      PGA                 <= Rx_control_3[2];   // decode ADC gain high or low
      DITHER              <= Rx_control_3[3];   // decode dither on or off
      RAND                <= Rx_control_3[4];   // decode randomizer on or off
      RX_relay            <= Rx_control_3[6:5]; // decode Alex Rx relays
      Rout                <= Rx_control_3[7];   // decode Alex Rx_1_out relay
      // RX_CONTROL_4
      TX_relay            <= Rx_control_4[1:0]; // decode Alex Tx Relays
    end
    else if (Rx_control_0[7:1] == 7'b0000_001)   // decode frequency 
      frequency <= {Rx_control_1, Rx_control_2, Rx_control_3, Rx_control_4};
  end
end	

assign PTT_out = Rx_control_0[0]; // Rx_control_0 only updated when we get correct sync sequence


//////////////////////////////////////////////////////////////
//
//   State Machine to manage PWM interface
//
//////////////////////////////////////////////////////////////
/*

    The code loops until there are at least 4 words in the Rx_FIFO.

    The first word is the Left audio followed by the Right audio
    which is followed by I data and finally the Q data.
    	
    The words sent to the D/A converters must be sent at the sample rate
    of the A/D converters (48kHz) so is synced on the negative edge of the CLRCLK.
*/

reg   [2:0] IF_PWM_state;      // state for PWM
reg   [2:0] IF_PWM_state_next; // next state for PWM
reg  [15:0] IF_Left_Data;      // Left 16 bit PWM data for D/A converter
reg  [15:0] IF_Right_Data;     // Right 16 bit PWM data for D/A converter
reg  [15:0] IF_I_PWM;          // I 16 bit PWM data for D/A conveter
reg  [15:0] IF_Q_PWM;          // Q 16 bit PWM data for D/A conveter
reg         IF_get_samples;
wire        IF_get_rx_data;
wire        IF_bleed;
reg  [11:0] IF_bleed_cnt;

// Bleed the RX FIFO if no data is being sent to Mercury/Janus or Penelope/Janus so that
// new RX Control data keeps coming in. Otherwise everything will come to a halt.  Have
// to keep getting RX Control data so we have new C & C data - specifically clock_s[].
// Bleed time is set to occur if the dead time is greater than one 24Khz period since
// CLRCLK is normally 48KHz.  Dead time can be different than one 24Khz period so long
// as it longer than one 48Khz period.  This problem occurs when switching 122.88Mhz clock
// sources
assign IF_get_rx_data = IF_get_samples || IF_bleed;

assign IF_bleed  = (IF_bleed_cnt == (48000000/24000));

localparam PWM_IDLE     = 0,
           PWM_START    = 1,
           PWM_LEFT     = 2,
           PWM_RIGHT    = 3,
           PWM_I_AUDIO  = 4,
           PWM_Q_AUDIO  = 5;

always @ (posedge IF_clk) 
begin
  if (IF_rst)
    IF_PWM_state   <= #IF_TPD PWM_IDLE;
  else
    IF_PWM_state   <= #IF_TPD IF_PWM_state_next;

  if (IF_rst)
    IF_bleed_cnt   <= #IF_TPD 1'b0;
  else if (IF_get_rx_data)
    IF_bleed_cnt   <= #IF_TPD 1'b0; // keep clearing IF_bleed count any time we get data from RX FIFO
  else
    IF_bleed_cnt   <= #IF_TPD IF_bleed_cnt + 1'b1;

  // get Left audio
  if (IF_PWM_state == PWM_LEFT)
    IF_Left_Data   <= #IF_TPD Rx_fifo_rdata;

  // get Right audio
  if (IF_PWM_state == PWM_RIGHT)
    IF_Right_Data  <= #IF_TPD Rx_fifo_rdata;

  // get I audio
  if (IF_PWM_state == PWM_I_AUDIO)
    IF_I_PWM       <= #IF_TPD Rx_fifo_rdata;

  // get Q audio
  if (IF_PWM_state == PWM_Q_AUDIO)
    IF_Q_PWM       <= #IF_TPD Rx_fifo_rdata;
end

always @*
begin
  case (IF_PWM_state)
    PWM_IDLE:
    begin
      Rx_fifo_rreq = 1'b0;

      if ((RX_USED > 3) && IF_get_rx_data)
        IF_PWM_state_next = PWM_START;   // time to get the donuts
      else
        IF_PWM_state_next = PWM_IDLE;    // or stay here if not enough data
    end

    // get ready
    PWM_START:
    begin
      Rx_fifo_rreq    = 1'b1;
      IF_PWM_state_next  = PWM_LEFT;
    end

    // get Left audio
    PWM_LEFT:
    begin
      Rx_fifo_rreq    = 1'b1;
      IF_PWM_state_next  = PWM_RIGHT;
    end

    // get Right audio
    PWM_RIGHT:
    begin
      Rx_fifo_rreq    = 1'b1;
      IF_PWM_state_next  = PWM_I_AUDIO;
    end

    // get I audio
    PWM_I_AUDIO:
    begin
      Rx_fifo_rreq    = 1'b1;
      IF_PWM_state_next  = PWM_Q_AUDIO;
    end

    // get Q audio
    PWM_Q_AUDIO:
    begin
      Rx_fifo_rreq    = 1'b0;
      IF_PWM_state_next  = PWM_IDLE;
    end

    default:
    begin
      Rx_fifo_rreq    = 1'b0;
      IF_PWM_state_next  = PWM_IDLE;
    end
  endcase
end

///////////////////////////////////////////////////////////////////////////////
//
// Left/Right Audio data transfers to Mercury(C24)/Janus(C4)
// I/Q Audio data transfer to Penelope(C19)/Janus(C12)
//
///////////////////////////////////////////////////////////////////////////////
reg        IF_xmit_req;
reg        IF_lrfall;
reg        IF_xmit_rdy;
wire       IF_m_rdy, IF_p_rdy;
reg        IF_j_rdy;
reg  [2:0] p, m, j;

always @(posedge IF_clk)
begin: SAMPLES
  if (IF_rst)
    IF_get_samples <= 1'b1; // get first set of data from RX FIFO before transmitter is ready
  else if (IF_xmit_req) 
    IF_get_samples <= 1'b1;  // falling edge of ack is a good time to start getting next set of data
  else if (IF_PWM_state == PWM_Q_AUDIO)
    IF_get_samples <= 1'b0;

  if (IF_rst)
    m <= 3'b000;
  else 
    m <= {m[1:0], C23}; // M_LR_sync

  if (IF_rst)
    p <= 3'b000;
  else 
    p <= {p[1:0], C22}; // P_IQ_sync

  if (IF_rst)
    j <= 3'b000;
  else 
    j <= {j[1:0], C12_CLRCLK};

  IF_xmit_req <= #IF_TPD IF_xmit_rdy & IF_lrfall;
end

always @*
begin
  if (conf[1])
    IF_lrfall = !m[1] & m[2];  // Mercury installed
  else if (conf[0])
    IF_lrfall = !p[1] & p[2];  // Penelope installed
  else
    IF_lrfall = !j[1] & j[2];  // use Janus
end

always @*
begin
  if (conf[1])
    IF_xmit_rdy = IF_m_rdy;  // Mercury installed
  else if (conf[0])
    IF_xmit_rdy = IF_p_rdy;  // Penelope installed
  else
    IF_xmit_rdy = IF_j_rdy;  // use Janus
end

wire C12_j_rdy;
always @(posedge IF_clk)
begin: C12_to_IF_domain
  reg rd0;
  
  if (IF_rst)
    {IF_j_rdy, rd0} <= 2'b00;
  else
    {IF_j_rdy, rd0} <= {rd0, C12_j_rdy};
end

reg C12_xmit_req;
always @(posedge C12_clk)
begin: IF_to_C12_domain
  reg rq0;
  
  if (C12_rst)
    {C12_xmit_req, rq0} <= 2'b00;
  else
    {C12_xmit_req, rq0} = {rq0, IF_xmit_req};
end

// 16 bits, two channels for PWM DAC on Mercury or Janus
NWire_xmit #(.SEND_FREQ(48000),.DATA_BITS(32), .ICLK_FREQ(144000000), .XCLK_FREQ(48000000), .LOW_TIME(1'b0))
  M_LRAudio (.irst(C144_rst), .iclk(C144_clk), .xrst(IF_rst), .xclk(IF_clk),
             .xdata({IF_Left_Data,IF_Right_Data}), .xreq(IF_xmit_req), .xrdy(IF_m_rdy),
             .xack(), .dout(C24));

I2S_xmit #(.DATA_BITS(32))
  J_LRAudio (.rst(C12_rst), .lrclk(C12_CLRCLK), .clk(C12_CBCLK), .CBrise(C12_cbrise),
             .CBfall(C12_cbfall), .sample({IF_Left_Data,IF_Right_Data}), .outbit(C4),
             .xmit_rdy(C12_j_rdy), .xmit_ack(C12_xmit_req));

// 16 bits, two channels for TLV320AIC23B D/A converter on Penelope or Janus
NWire_xmit #(.SEND_FREQ(48000),.DATA_BITS(32), .ICLK_FREQ(144000000), .XCLK_FREQ(48000000), .LOW_TIME(1'b0))
  P_IQPWM (.irst(C144_rst), .iclk(C144_clk), .xrst(IF_rst), .xclk(IF_clk),
           .xdata({IF_I_PWM,IF_Q_PWM}), .xreq(IF_xmit_req), .xrdy(IF_p_rdy),
           .xack(), .dout(C19) );

I2S_xmit #(.DATA_BITS(32))
  J_IQPWM (.rst(C12_rst), .lrclk(C12_CLRCLK), .clk(C12_CBCLK), .CBrise(C12_cbrise),
           .CBfall(C12_cbfall), .sample({IF_I_PWM,IF_Q_PWM}), .outbit(C12),
           .xmit_rdy(), .xmit_ack(C12_xmit_req));

///////////////////////////////////////////////////////////////
//
//              Implements Command & Control  encoder 
//
///////////////////////////////////////////////////////////////
/*
	The C&C encoder broadcasts data over the Atlas bus C20 for
	use by other cards e.g. Mercury and Penelope.
	
	The data fomat is as follows:
	
	<[60:59]DFS1,DFS0><[58]PTT><[57:54]address><[53:22]frequency><[21:18]clock_select><[17:11]OC>
	<[10]Mode><[9]PGA><[8]DITHER><[7]RAND><[6:5]ATTEN><[4:3]TX_relay><[2]Rout><[1:0]RX_relay> 
	
	Total of 61 bits. Frequency is in Hz and OC is the open collector data on Penelope.
	The clock source decodes as follows:
	
	0x00  = 10MHz reference from Atlas bus ie Gibraltar
	0x01  = 10MHz reference from Penelope
	0x10  = 10MHz reference from Mercury
	00xx  = 122.88MHz source from Penelope 
	01xx  = 122.88MHz source from Mercury

		
	For future expansion the four bit address enables specific C&C data to be send to individual boards.
	For the present for use with Mercury and Penelope the address is ignored. 

*/
wire [60:0] xmit_data;
wire  [3:0] CC_address;     // C&C address, fixed for now 

// dummy address data for now 
assign CC_address = 4'b0000;

assign xmit_data = {IF_DFS1,IF_DFS0,PTT_out,CC_address,frequency,IF_clock_s,OC,mode,PGA,
                    DITHER,RAND,ATTEN, TX_relay, Rout, RX_relay};

NWire_xmit  #(.DATA_BITS(61), .ICLK_FREQ(48000000), .XCLK_FREQ(48000000), .SEND_FREQ(1000)) 
      CCxmit (.irst(IF_rst), .iclk(IF_clk), .xrst(IF_rst), .xclk(IF_clk),
              .xdata(xmit_data), .xreq(1'b1), .xrdy(), .xack(), .dout(CC));
///////////////////////////////////////////////////////
//
//                      NWire Penelope data
//
///////////////////////////////////////////////////////

// Gets current software serial # as an 8 bit value
// format. Sends ALC as 12 bits

wire [19:0] pd;
wire        p_rdy;

always @(posedge IF_clk)
begin
  if (p_rdy)
    {Penny_serialno, Penny_ALC} <= #IF_TPD pd;
end

NWire_rcv  #(.DATA_BITS(20), .ICLK_FREQ(48000000), .XCLK_FREQ(48000000), .SLOWEST_FREQ(500)) 
      p_ser (.irst(IF_rst), .iclk(IF_clk), .xrst(IF_rst), .xclk(IF_clk),
             .xrcv_data(pd), .xrcv_rdy(p_rdy), .xrcv_ack(p_rdy), .din(A5));
///////////////////////////////////////////////////////
//
//                      NWire Mercury data
//
///////////////////////////////////////////////////////

wire  [8:0] md;
wire        m_rdy;

always @(posedge IF_clk)
begin
  if (m_rdy)
    {Merc_serialno, ADC_OVERLOAD} <= #IF_TPD md;
end

NWire_rcv  #(.DATA_BITS(9), .ICLK_FREQ(48000000), .XCLK_FREQ(48000000), .SLOWEST_FREQ(500)) 
      m_ser (.irst(IF_rst), .iclk(IF_clk), .xrst(IF_rst), .xclk(IF_clk),
             .xrcv_data(md), .xrcv_rdy(m_rdy), .xrcv_ack(m_rdy), .din(A6));
///////////////////////////////////////////////////////
//
//                      Debounce PTT button
//
///////////////////////////////////////////////////////


debounce de_PTT(.clean_pb(clean_PTT_in), .pb(PTT_in), .clk(IF_clk));



///////////////////////////////////////////////////////
//
//                      Debounce dot key - active low
//
///////////////////////////////////////////////////////


debounce de_dot(.clean_pb(clean_dot), .pb(~dot_n), .clk(IF_clk));


///////////////////////////////////////////////////////
//
//                      Debounce dash key - active low
//
///////////////////////////////////////////////////////


debounce de_dash(.clean_pb(clean_dash), .pb(~dash_n), .clk(IF_clk));


wire led0_off;
wire led3_off;

// Flash the LEDs to show something is working! - LEDs are active low

assign DEBUG_LED0 = led0_off; //D1 LED
assign DEBUG_LED1 = ~conf[1];	// test config setting  
assign DEBUG_LED2 = ~PTT_out; // lights with PTT active
assign DEBUG_LED3 = led3_off; // D4 LED 
//assign DEBUG_LED1 = (Rx_control_0[7:1] == 0) ?  Rx_control_3[0] : DEBUG_LED1;
//assign DEBUG_LED2 = (Rx_control_0[7:1] == 0) ?  Rx_control_3[1] : DEBUG_LED2;

wire [2:0] err_sigs;
wire [1:0] sync_err;

reg [23:0] LRcnt;  // just for debuggin purposes to see how long a particular signal is high or low
always @(posedge IF_clk)
begin
  if (IF_rst)
    LRcnt <= 0;
//  else if (FLAGC)
  else if (IF_Audio_Data_rdy)
    LRcnt <= 0;
  else
    LRcnt <= LRcnt + 1'b1;    // how long the signal is low  
end

assign err_sigs = {(LRcnt > 24'h60000),Rx_fifo_full, Tx_fifo_full & Tx_fifo_wreq};
assign sync_err[0] = (SYNC_state == SYNC_START) && Rx_fifo_drdy && (Rx_fifo_wdata[15:8] != 8'h7F);
assign sync_err[1] = (SYNC_state == SYNC_IDLE) && Rx_fifo_drdy && (Rx_fifo_wdata != 16'h7F7F); // sync error

led_blinker #(3, 48000000) BLINK_D1 (IF_clk, err_sigs, led0_off);
led_blinker #(2, 48000000) BLINK_D4 (IF_clk, sync_err, led3_off);

function integer clogb2;
input [31:0] depth;
begin
  for(clogb2=0; depth>0; clogb2=clogb2+1)
  depth = depth >> 1;
end
endfunction

endmodule

