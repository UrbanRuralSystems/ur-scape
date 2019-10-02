// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   6.3.1.3 Linear Units Codes: http://geotiff.maptools.org/spec/geotiff6.html#6.3.1.3

public enum LinearUnit : ushort
{
	Undefined = 0,

	Meter = 9001,
	Foot = 9002,
	Foot_US_Survey = 9003,
	Foot_Modified_American = 9004,
	Foot_Clarke = 9005,
	Foot_Indian = 9006,
	Link = 9007,
	Link_Benoit = 9008,
	Link_Sears = 9009,
	Chain_Benoit = 9010,
	Chain_Sears = 9011,
	Yard_Sears = 9012,
	Yard_Indian = 9013,
	Fathom = 9014,
	Mile_International_Nautical = 9015,

	UserDefined = 32767
}
