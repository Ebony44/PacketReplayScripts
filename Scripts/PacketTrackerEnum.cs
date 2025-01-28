using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum EGAMELIST
{
    UNSELECTED = 0,
    GAME01_POKDENG,
    GAME02_GAOGEO,
    GAME03_HOLDEM,
    GAME04_BIGTWO,
    GAME05_THIRTEENPOKER,
    GAME06_PAIKAENG,
    GAME07_DOMINO,
    // GAME08_POKDENGPLUS,
    GAME09_ANIMALDICE = 9,
    GAME10_MIXEDTEN,

    COUNT_MAX = 99,
}
[System.Serializable]
public class TrackedGamePacketInfo
{
    public DateTime packetTime;
    public ByteBuffer[] byteBuffers; // = new ByteBuffer[];
    public int packetNumber;

    // public int stringStartPos = -1;
    // public int stringEndPos = -1;
}

public class PacketTrackerEnum
{

}
