// Copyright (C) 1991-2006 Altera Corporation
// Your use of Altera Corporation's design tools, logic functions 
// and other software and tools, and its AMPP partner logic 
// functions, and any output files any of the foregoing 
// (including device programming or simulation files), and any 
// associated documentation or information are expressly subject 
// to the terms and conditions of the Altera Program License 
// Subscription Agreement, Altera MegaCore Function License 
// Agreement, or other applicable license agreement, including, 
// without limitation, that your use is for the sole purpose of 
// programming logic devices manufactured by Altera and sold by 
// Altera or its authorized distributors.  Please refer to the 
// applicable agreement for further details.

module pfd(
	ref_in,
	osc_in,
	pfd_out
);

input	ref_in;
input	osc_in;
output	pfd_out;

reg	SYNTHESIZED_WIRE_8;
wire	SYNTHESIZED_WIRE_9;
wire	SYNTHESIZED_WIRE_1;
wire	SYNTHESIZED_WIRE_10;
reg	SYNTHESIZED_WIRE_11;
reg	SYNTHESIZED_WIRE_12;
wire	SYNTHESIZED_WIRE_6;
wire	SYNTHESIZED_WIRE_7;
reg	DFF_inst3;




assign	SYNTHESIZED_WIRE_1 = ~(SYNTHESIZED_WIRE_8 & SYNTHESIZED_WIRE_9);
assign	pfd_out = ~(SYNTHESIZED_WIRE_1 & SYNTHESIZED_WIRE_10);
assign	SYNTHESIZED_WIRE_9 = SYNTHESIZED_WIRE_11 ^ SYNTHESIZED_WIRE_12;

always@(posedge ref_in or negedge SYNTHESIZED_WIRE_8)
begin
if (!SYNTHESIZED_WIRE_8)
	begin
	DFF_inst3 <= 0;
	end
else
	begin
	DFF_inst3 <= SYNTHESIZED_WIRE_9;
	end
end

always@(posedge osc_in or negedge SYNTHESIZED_WIRE_10)
begin
if (!SYNTHESIZED_WIRE_10)
	begin
	SYNTHESIZED_WIRE_8 <= 1;
	end
else
	begin
	SYNTHESIZED_WIRE_8 <= SYNTHESIZED_WIRE_9;
	end
end

always@(posedge ref_in)
begin
	begin
	SYNTHESIZED_WIRE_11 <= SYNTHESIZED_WIRE_6;
	end
end

always@(posedge osc_in)
begin
	begin
	SYNTHESIZED_WIRE_12 <= SYNTHESIZED_WIRE_7;
	end
end
assign	SYNTHESIZED_WIRE_6 =  ~SYNTHESIZED_WIRE_11;
assign	SYNTHESIZED_WIRE_7 =  ~SYNTHESIZED_WIRE_12;
assign	SYNTHESIZED_WIRE_10 =  ~DFF_inst3;


endmodule
