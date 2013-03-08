/* --------------------------------------------------------------------------

   Copyright (C) 2004 Sean Cier
   Copyright (C) 2000 Robert Kaye

   This library is free software; you can redistribute it and/or
   modify it under the terms of the GNU Lesser General Public
   License as published by the Free Software Foundation; either
   version 2.1 of the License, or (at your option) any later version.

   This library is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
   Lesser General Public License for more details.

   You should have received a copy of the GNU Lesser General Public
   License along with this library; if not, write to the Free Software
   Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

----------------------------------------------------------------------------*/
namespace musicbrainz {

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

public class MusicBrainz {
  [StructLayout(LayoutKind.Sequential)]
  public struct BitprintInfo_Native {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst=255)]
    public byte[]     filename;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst=89)]
    public byte[]     bitprint;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst=41)]
    public byte[]     first20;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst=41)]
    public byte[]     audioSha1;
    public uint       length;
    public uint       duration;
    public uint       samplerate;
    public uint       bitrate;
    public byte       stereo;
    public byte       vbr;
  }

  private interface WindowsNetworkControlInterface {
    void Init(IntPtr musicBrainzObject);
    void Stop(IntPtr musicBrainzObject);
  }
  private class WindowsNetworkControl : WindowsNetworkControlInterface {
    [DllImport("musicbrainz")]
    private static extern int mb_WSAInit(IntPtr o);
    public void Init(IntPtr musicBrainzObject) {
      mb_WSAInit(musicBrainzObject);
    }
                                                                                
    [DllImport("musicbrainz")]
    private static extern int mb_WSAStop(IntPtr o);
    public void Stop(IntPtr musicBrainzObject) {
      mb_WSAStop(musicBrainzObject);
    }
  }
                                                                                
  private IntPtr musicBrainzObject;

  private static readonly Encoding UTF8_ENCODING  = new UTF8Encoding();
  private static int MAX_STRING_LEN = 8192;

  public static readonly int CDINDEX_ID_LEN = 28;
  public static readonly int ID_LEN = 36;

  [DllImport("musicbrainz")]
  private static extern IntPtr mb_New();
  [DllImport("musicbrainz")]
  private static extern void mb_Delete(IntPtr o);
  [DllImport("musicbrainz")]
  private static extern void mb_UseUTF8(IntPtr o, int value);

  public MusicBrainz() {
    musicBrainzObject = mb_New();
    mb_UseUTF8(musicBrainzObject, 1);
    BeginSession();
  }

  ~MusicBrainz() {
    EndSession();
    mb_Delete(musicBrainzObject);
  }

  private static bool IsWindows() {
    PlatformID platform = System.Environment.OSVersion.Platform;
    PlatformID winPlatformMask =
      PlatformID.Win32S | PlatformID.Win32Windows | PlatformID.Win32NT;
    return (int)(platform & winPlatformMask) != 0;
  }

  private static byte[] ToUTF8(String s) {
    if (s == null) {
      return null;
    }
    int len = UTF8_ENCODING.GetByteCount(s);
    byte[] result = new byte[len+1];
    UTF8_ENCODING.GetBytes(s, 0, s.Length, result, 0);
    result[len] = 0;
    return result;
  }

  private static String FromUTF8(byte[] bytes) {
    if (bytes == null) {
      return null;
    }
    int len = 0;
    while ((len < bytes.Length) && (bytes[len] != 0)) {
      len++;
    }
    return UTF8_ENCODING.GetString(bytes, 0, len);
  }

  private static IntPtr ToUTF8Native(String s) {
    if (s == null) {
      return (IntPtr)0;
    }
    byte[] bytes = UTF8_ENCODING.GetBytes(s);
    IntPtr nativeBytes = Marshal.AllocHGlobal(bytes.Length + 1);
    Marshal.Copy(bytes, 0, nativeBytes, bytes.Length);
    Marshal.WriteByte(nativeBytes, bytes.Length, (byte)0);
    return nativeBytes;
  }
                                                                                
  private static void FreeUTF8Native(IntPtr nativeBytes) {
    if (nativeBytes == (IntPtr)0) {
      return;
    }
    Marshal.FreeHGlobal(nativeBytes);
  }
                                                                                
  private static String FromUTF8Native(IntPtr nativeBytes, int maxLen) {
    if (nativeBytes == (IntPtr)0) {
      return null;
    }
    byte[] bytes = new byte[maxLen];
    Marshal.Copy(nativeBytes, bytes, 0, maxLen);
    return FromUTF8(bytes);
  }

  private void BeginSession() {
    if (IsWindows()) {
      Type t = Type.GetType("musicbrainz.MusicBrainz+WindowsNetworkControl");
      Object o = t.GetConstructor(new Type[0]).Invoke(new Object[0]);
      ((WindowsNetworkControlInterface)o).Init(musicBrainzObject);
    }
  }
                                                                                
  private void EndSession() {
    if (IsWindows()) {
      Type t = Type.GetType("musicbrainz.MusicBrainz+WindowsNetworkControl");
      Object o = t.GetConstructor(new Type[0]).Invoke(new Object[0]);
      ((WindowsNetworkControlInterface)o).Stop(musicBrainzObject);
    }
  }
                                                                                
  [DllImport("musicbrainz")]
  private static extern void mb_GetVersion(IntPtr o,
                                           out int major,
                                           out int minor,
                                           out int rev);
  public void GetVersion(out int major, out int minor, out int rev) {
    mb_GetVersion(musicBrainzObject,
                  out major, out minor, out rev);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_SetServer(IntPtr o,
                                         byte[] serverAddr,
                                         short serverPort);
  public bool SetServer(String serverAddr, short serverPort) {
    int result =
      mb_SetServer(musicBrainzObject, ToUTF8(serverAddr), serverPort);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_SetProxy(IntPtr o,
                                        byte[] serverAddr,
                                        short serverPort);
  public bool SetProxy(String serverAddr, short serverPort) {
    int result = mb_SetProxy(musicBrainzObject, ToUTF8(serverAddr), serverPort);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_Authenticate(IntPtr o,
                                            byte[] userName,
                                            byte[] password);
  public bool Authenticate(String userName, String password) {
    int result =
      mb_Authenticate(musicBrainzObject, ToUTF8(userName), ToUTF8(password));

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_SetDevice(IntPtr o,
                                         byte[] device);
  public bool SetDevice(String device) {
    int result = mb_SetDevice(musicBrainzObject, ToUTF8(device));

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern void mb_SetDepth(IntPtr o, int depth);
  public void SetDepth(int depth) {
    mb_SetDepth(musicBrainzObject, depth);
  }

  [DllImport("musicbrainz")]
  private static extern void mb_SetMaxItems(IntPtr o, int maxItems);
  public void SetMaxItems(int maxItems) {
    mb_SetMaxItems(musicBrainzObject, maxItems);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_Query(IntPtr o,
                                     byte[] rdfObject);
  public bool Query(String rdfObject) {
    int result = mb_Query(musicBrainzObject, ToUTF8(rdfObject));

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_QueryWithArgs(IntPtr o,
                                             byte[] rdfObject,
                                             IntPtr[] args);
  public bool Query(String rdfObject, String[] args) {
    IList argsList = new ArrayList();
    foreach (String s in args) {
      argsList.Add(s);
    }
    return Query(rdfObject, argsList);
  }
  public bool Query(String rdfObject, IList args) {
    if (args == null) {
      return Query(rdfObject);
    }

    IntPtr[] argsNative = new IntPtr[args.Count+1];
    for (int i = 0; i < args.Count; i++) {
      argsNative[i] = ToUTF8Native((String)args[i]);
    }
    argsNative[args.Count] = (IntPtr)0;

    int result =
      mb_QueryWithArgs(musicBrainzObject, ToUTF8(rdfObject), argsNative);

    for (int i = 0; i < args.Count; i++) {
      FreeUTF8Native(argsNative[i]);
    }

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_GetQueryError(IntPtr o,
                                             byte[] error,
                                             int errorLen);
  public bool GetQueryError(out String error) {
    byte[] errorNative = new byte[MAX_STRING_LEN];

    int result =
      mb_GetQueryError(musicBrainzObject, errorNative, MAX_STRING_LEN);
    error = (result == 0) ? null : FromUTF8(errorNative);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_GetWebSubmitURL(IntPtr o,
                                               byte[] url,
                                               int urlLen);
  public bool GetWebSubmitURL(out String url) {
    byte[] urlNative = new byte[MAX_STRING_LEN];

    int result =
      mb_GetWebSubmitURL(musicBrainzObject, urlNative, MAX_STRING_LEN);
    url = (result == 0) ? null : FromUTF8(urlNative);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_Select1(IntPtr o,
                                       byte[] selectQuery,
                                       int ordinal);
  public bool Select(String selectQuery) {
    return Select(selectQuery, 0);
  }
  public bool Select(String selectQuery, int index) {
    int result = mb_Select1(musicBrainzObject, ToUTF8(selectQuery), index);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_SelectWithArgs(IntPtr o,
                                              byte[] selectQuery,
                                              int[] ordinals);
  public bool Select(String selectQuery, IList indices) {
    int[] indexArray = new int[indices.Count];
    for (int i = 0; i < indices.Count; i++) {
      indexArray[i] = (int)indices[i];
    }
    return Select(selectQuery, indexArray);
  }
  public bool Select(String selectQuery, int[] indices) {
    int result =
      mb_SelectWithArgs(musicBrainzObject, ToUTF8(selectQuery), indices);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_DoesResultExist1(IntPtr o,
                                                byte[] resultName,
                                                int ordinal);
  public bool DoesResultExist(String resultName) {
    return DoesResultExist(resultName, 0);
  }
  public bool DoesResultExist(String resultName, int index) {
    int result =
      mb_DoesResultExist1(musicBrainzObject, ToUTF8(resultName), index);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_GetResultData1(IntPtr o,
                                              byte[] resultName,
                                              byte[] data,
                                              int dataLen,
                                              int ordinal);
  public String Data(String resultName) {
    return Data(resultName, 0);
  }
  public String Data(String resultName, int index) {
    String result;
    return GetResultData(resultName, index, out result) ? result : null;
  }
  public bool GetResultData(String resultName, out String data) {
    return GetResultData(resultName, 0, out data);
  }
  public bool GetResultData(String resultName, int index, out String data) {
    byte[] dataNative = new byte[MAX_STRING_LEN];
    int result = mb_GetResultData1(musicBrainzObject,
                                   ToUTF8(resultName),
                                   dataNative, MAX_STRING_LEN,
                                   index);
    data = (result == 0) ? null : FromUTF8(dataNative);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_GetResultInt1(IntPtr o,
                                             byte[] resultName,
                                             int ordinal);
  public int DataInt(String resultName) {
    return DataInt(resultName, 0);
  }
  public int DataInt(String resultName, int index) {
    return GetResultInt(resultName, index);
  }
  public int GetResultInt(String resultName) {
    return GetResultInt(resultName, 0);
  }
  public int GetResultInt(String resultName, int index) {
    int result = mb_GetResultInt1(musicBrainzObject,
                                  ToUTF8(resultName),
                                  index);

    return result;
  }

  [DllImport("musicbrainz")]
  private static extern int mb_GetResultRDF(IntPtr o,
                                            byte[] rdf,
                                            int rdfLen);
  [DllImport("musicbrainz")]
  private static extern int mb_GetResultRDFLen(IntPtr o);
  public bool GetResultRDF(out String rdfObject) {
    int len = (int)mb_GetResultRDFLen(musicBrainzObject);

    byte[] rdfObjectNative = new byte[len+1];
    int result = mb_GetResultRDF(musicBrainzObject, rdfObjectNative, len+1);
    rdfObject = (result == 0) ? null : FromUTF8(rdfObjectNative);

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_SetResultRDF(IntPtr o,
                                            byte[] rdf);
  public bool SetResultRDF(String rdf) {
    int result = mb_SetResultRDF(musicBrainzObject, ToUTF8(rdf));

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern void mb_GetIDFromURL(IntPtr o,
                                             byte[] url,
                                             byte[] id,
                                             int idLen);
  public void GetIDFromURL(String url, out String id) {
    byte[] idNative = new byte[MAX_STRING_LEN];
    mb_GetIDFromURL(musicBrainzObject, ToUTF8(url), idNative, MAX_STRING_LEN);
    id = FromUTF8(idNative);
  }

  [DllImport("musicbrainz")]
  private static extern void mb_GetFragmentFromURL(IntPtr o,
                                                   byte[] url,
                                                   byte[] fragment,
                                                   int fragmentLen);
  public void GetFragmentFromURL(String url, out String fragment) {
    byte[] fragmentNative = new byte[MAX_STRING_LEN];

    mb_GetFragmentFromURL(musicBrainzObject,
                          ToUTF8(url),
                          fragmentNative, MAX_STRING_LEN);
    fragment = FromUTF8(fragmentNative);
  }

  [DllImport("musicbrainz")]
  private static extern int mb_GetOrdinalFromList(IntPtr o,
                                                  byte[] resultList,
                                                  byte[] uri);
  public int GetOrdinalFromList(String resultList, String uri) {
    int result =
      mb_GetOrdinalFromList(musicBrainzObject, ToUTF8(resultList), ToUTF8(uri));

    return result;
  }

  [DllImport("musicbrainz")]
  private static extern int mb_GetMP3Info(IntPtr o,
                                          byte[] fileName,
                                          out int duration,
                                          out int bitrate,
                                          out int stereo,
                                          out int samplerate);
  public bool GetMP3Info(String fileName,
                         out int duration,
                         out int bitrate,
                         out bool stereo,
                         out int samplerate) {
    int stereoNative;
    int result = mb_GetMP3Info(musicBrainzObject,
                               ToUTF8(fileName),
                               out duration,
                               out bitrate,
                               out stereoNative,
                               out samplerate);
    stereo = stereoNative != 0;

    return (result != 0);
  }

  [DllImport("musicbrainz")]
  private static extern void mb_SetDebug(IntPtr o, int debug);
  public void SetDebug(bool debug) {
    mb_SetDebug(musicBrainzObject, debug ? 1 : 0);
  }

  // ========= Query Constants ==========

  public static readonly String
    MBI_VARIOUS_ARTIST_ID =
      "89ad4ac3-39f7-470e-963a-56509c546377",
    MBS_Rewind =
      "[REWIND]",
    MBS_Back =
      "[BACK]",
    MBS_SelectArtist =
      "http://musicbrainz.org/mm/mm-2.1#artistList []",
    MBS_SelectAlbum =
      "http://musicbrainz.org/mm/mm-2.1#albumList []",
    MBS_SelectTrack =
      "http://musicbrainz.org/mm/mm-2.1#trackList []",
    MBS_SelectTrackArtist =
      "http://purl.org/dc/elements/1.1/creator",
    MBS_SelectTrackAlbum =
      "http://musicbrainz.org/mm/mq-1.1#album",
    MBS_SelectTrmid =
      "http://musicbrainz.org/mm/mm-2.1#trmidList []",
    MBS_SelectCdindexid =
      "http://musicbrainz.org/mm/mm-2.1#cdindexidList []",
    MBS_SelectReleaseDate =
      "http://musicbrainz.org/mm/mm-2.1#releaseDateList []",
    MBS_SelectLookupResult =
      "http://musicbrainz.org/mm/mq-1.1#lookupResultList []",
    MBS_SelectLookupResultArtist =
      "http://musicbrainz.org/mm/mq-1.1#artist",
    MBS_SelectLookupResultAlbum =
      "http://musicbrainz.org/mm/mq-1.1#album",
    MBS_SelectLookupResultTrack =
      "http://musicbrainz.org/mm/mq-1.1#track",
    MBE_GetStatus =
      "http://musicbrainz.org/mm/mq-1.1#status",
    MBE_GetNumArtists =
      "http://musicbrainz.org/mm/mm-2.1#artistList [COUNT]",
    MBE_GetNumAlbums =
      "http://musicbrainz.org/mm/mm-2.1#albumList [COUNT]",
    MBE_GetNumTracks =
      "http://musicbrainz.org/mm/mm-2.1#trackList [COUNT]",
    MBE_GetNumTrmids =
      "http://musicbrainz.org/mm/mm-2.1#trmidList [COUNT]",
    MBE_GetNumLookupResults =
      "http://musicbrainz.org/mm/mm-2.1#lookupResultList [COUNT]",
    MBE_ArtistGetArtistName =
      "http://purl.org/dc/elements/1.1/title",
    MBE_ArtistGetArtistSortName =
      "http://musicbrainz.org/mm/mm-2.1#sortName",
    MBE_ArtistGetArtistId =
      "",
    MBE_ArtistGetAlbumName =
      "http://musicbrainz.org/mm/mm-2.1#albumList [] http://purl.org/dc/elements/1.1/title",
    MBE_ArtistGetAlbumId =
      "http://musicbrainz.org/mm/mm-2.1#albumList []",
    MBE_AlbumGetAlbumName =
      "http://purl.org/dc/elements/1.1/title",
    MBE_AlbumGetAlbumId =
      "",
    MBE_AlbumGetAlbumStatus =
      "http://musicbrainz.org/mm/mm-2.1#releaseStatus",
    MBE_AlbumGetAlbumType =
      "http://musicbrainz.org/mm/mm-2.1#releaseType",
    MBE_AlbumGetAmazonAsin =
      "http://www.amazon.com/gp/aws/landing.html#Asin",
    MBE_AlbumGetAmazonCoverartURL =
      "http://musicbrainz.org/mm/mm-2.1#coverart",
    MBE_AlbumGetNumCdindexIds =
      "http://musicbrainz.org/mm/mm-2.1#cdindexidList [COUNT]",
    MBE_AlbumGetNumReleaseDates =
      "http://musicbrainz.org/mm/mm-2.1#releaseDateList [COUNT]",
    MBE_AlbumGetAlbumArtistId =
      "http://purl.org/dc/elements/1.1/creator",
    MBE_AlbumGetNumTracks =
      "http://musicbrainz.org/mm/mm-2.1#trackList [COUNT]",
    MBE_AlbumGetTrackId =
      "http://musicbrainz.org/mm/mm-2.1#trackList [] ",
    MBE_AlbumGetTrackList =
      "http://musicbrainz.org/mm/mm-2.1#trackList",
    MBE_AlbumGetTrackNum =
      "http://musicbrainz.org/mm/mm-2.1#trackList [?] http://musicbrainz.org/mm/mm-2.1#trackNum",
    MBE_AlbumGetTrackName =
      "http://musicbrainz.org/mm/mm-2.1#trackList [] http://purl.org/dc/elements/1.1/title",
    MBE_AlbumGetTrackDuration =
      "http://musicbrainz.org/mm/mm-2.1#trackList [] http://musicbrainz.org/mm/mm-2.1#duration",
    MBE_AlbumGetArtistName =
      "http://musicbrainz.org/mm/mm-2.1#trackList [] http://purl.org/dc/elements/1.1/creator http://purl.org/dc/elements/1.1/title",
    MBE_AlbumGetArtistSortName =
      "http://musicbrainz.org/mm/mm-2.1#trackList [] http://purl.org/dc/elements/1.1/creator http://musicbrainz.org/mm/mm-2.1#sortName",
    MBE_AlbumGetArtistId =
      "http://musicbrainz.org/mm/mm-2.1#trackList [] http://purl.org/dc/elements/1.1/creator",
    MBE_TrackGetTrackName =
      "http://purl.org/dc/elements/1.1/title",
    MBE_TrackGetTrackId =
      "",
    MBE_TrackGetTrackNum =
      "http://musicbrainz.org/mm/mm-2.1#trackNum",
    MBE_TrackGetTrackDuration =
      "http://musicbrainz.org/mm/mm-2.1#duration",
    MBE_TrackGetArtistName =
      "http://purl.org/dc/elements/1.1/creator http://purl.org/dc/elements/1.1/title",
    MBE_TrackGetArtistSortName =
      "http://purl.org/dc/elements/1.1/creator http://musicbrainz.org/mm/mm-2.1#sortName",
    MBE_TrackGetArtistId =
      "http://purl.org/dc/elements/1.1/creator",
    MBE_QuickGetArtistName =
      "http://musicbrainz.org/mm/mq-1.1#artistName",
    MBE_QuickGetArtistSortName =
      "http://musicbrainz.org/mm/mm-2.1#sortName",
    MBE_QuickGetArtistId =
      "http://musicbrainz.org/mm/mm-2.1#artistid",
    MBE_QuickGetAlbumName =
      "http://musicbrainz.org/mm/mq-1.1#albumName",
    MBE_QuickGetTrackName =
      "http://musicbrainz.org/mm/mq-1.1#trackName",
    MBE_QuickGetTrackNum =
      "http://musicbrainz.org/mm/mm-2.1#trackNum",
    MBE_QuickGetTrackId =
      "http://musicbrainz.org/mm/mm-2.1#trackid",
    MBE_QuickGetTrackDuration =
      "http://musicbrainz.org/mm/mm-2.1#duration",
    MBE_ReleaseGetDate =
      "http://purl.org/dc/elements/1.1/date",
    MBE_ReleaseGetCountry =
      "http://musicbrainz.org/mm/mm-2.1#country",
    MBE_LookupGetType =
      "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
    MBE_LookupGetRelevance =
      "http://musicbrainz.org/mm/mq-1.1#relevance",
    MBE_LookupGetArtistId =
      "http://musicbrainz.org/mm/mq-1.1#artist",
    MBE_LookupGetAlbumId =
      "http://musicbrainz.org/mm/mq-1.1#album",
    MBE_LookupGetAlbumArtistId =
      "http://musicbrainz.org/mm/mq-1.1#album " +
      "http://purl.org/dc/elements/1.1/creator",
    MBE_LookupGetTrackId =
      "http://musicbrainz.org/mm/mq-1.1#track",
    MBE_LookupGetTrackArtistId =
      "http://musicbrainz.org/mm/mq-1.1#track " +
      "http://purl.org/dc/elements/1.1/creator",
    MBE_TOCGetCDIndexId =
      "http://musicbrainz.org/mm/mm-2.1#cdindexid",
    MBE_TOCGetFirstTrack =
      "http://musicbrainz.org/mm/mm-2.1#firstTrack",
    MBE_TOCGetLastTrack =
      "http://musicbrainz.org/mm/mm-2.1#lastTrack",
    MBE_TOCGetTrackSectorOffset =
      "http://musicbrainz.org/mm/mm-2.1#toc [] http://musicbrainz.org/mm/mm-2.1#sectorOffset",
    MBE_TOCGetTrackNumSectors =
      "http://musicbrainz.org/mm/mm-2.1#toc [] http://musicbrainz.org/mm/mm-2.1#numSectors",
    MBE_AuthGetSessionId =
      "http://musicbrainz.org/mm/mq-1.1#sessionId",
    MBE_AuthGetChallenge =
      "http://musicbrainz.org/mm/mq-1.1#authChallenge",
    MBQ_GetCDInfo =
      "@CDINFO@",
    MBQ_GetCDTOC =
      "@LOCALCDINFO@",
    MBQ_AssociateCD =
      "@CDINFOASSOCIATECD@",
    MBQ_Authenticate =
      "<mq:AuthenticateQuery>\n" +
      "   <mq:username>@1@</mq:username>\n" +
      "</mq:AuthenticateQuery>\n",
    MBQ_GetCDInfoFromCDIndexId =
      "<mq:GetCDInfo>\n" +
      "   <mq:depth>@DEPTH@</mq:depth>\n" +
      "   <mm:cdindexid>@1@</mm:cdindexid>\n" +
      "</mq:GetCDInfo>\n",
    MBQ_TrackInfoFromTRMId =
      "<mq:TrackInfoFromTRMId>\n" +
      "   <mm:trmid>@1@</mm:trmid>\n" +
      "   <mq:artistName>@2@</mq:artistName>\n" +
      "   <mq:albumName>@3@</mq:albumName>\n" +
      "   <mq:trackName>@4@</mq:trackName>\n" +
      "   <mm:trackNum>@5@</mm:trackNum>\n" +
      "   <mm:duration>@6@</mm:duration>\n" +
      "</mq:TrackInfoFromTRMId>\n",
    MBQ_QuickTrackInfoFromTrackId =
      "<mq:QuickTrackInfoFromTrackId>\n" +
      "   <mm:trackid>@1@</mm:trackid>\n" +
      "   <mm:albumid>@2@</mm:albumid>\n" +
      "</mq:QuickTrackInfoFromTrackId>\n",
    MBQ_FindArtistByName =
      "<mq:FindArtist>\n" +
      "   <mq:depth>@DEPTH@</mq:depth>\n" +
      "   <mq:artistName>@1@</mq:artistName>\n" +
      "   <mq:maxItems>@MAX_ITEMS@</mq:maxItems>\n" +
      "</mq:FindArtist>\n",
    MBQ_FindAlbumByName =
      "<mq:FindAlbum>\n" +
      "   <mq:depth>@DEPTH@</mq:depth>\n" +
      "   <mq:maxItems>@MAX_ITEMS@</mq:maxItems>\n" +
      "   <mq:albumName>@1@</mq:albumName>\n" +
      "</mq:FindAlbum>\n",
    MBQ_FindTrackByName =
      "<mq:FindTrack>\n" +
      "   <mq:depth>@DEPTH@</mq:depth>\n" +
      "   <mq:maxItems>@MAX_ITEMS@</mq:maxItems>\n" +
      "   <mq:trackName>@1@</mq:trackName>\n" +
      "</mq:FindTrack>\n",
    MBQ_FindDistinctTRMId =
      "<mq:FindDistinctTRMID>\n" +
      "   <mq:depth>@DEPTH@</mq:depth>\n" +
      "   <mq:artistName>@1@</mq:artistName>\n" +
      "   <mq:trackName>@2@</mq:trackName>\n" +
      "</mq:FindDistinctTRMID>\n",
    MBQ_GetArtistById =
      "http://@URL@/mm-2.1/artist/@1@/@DEPTH@",
    MBQ_GetAlbumById =
      "http://@URL@/mm-2.1/album/@1@/@DEPTH@",
    MBQ_GetTrackById =
      "http://@URL@/mm-2.1/track/@1@/@DEPTH@",
    MBQ_GetTrackByTRMId =
      "http://@URL@/mm-2.1/trmid/@1@/@DEPTH@",
    MBQ_SubmitTrackTRMId =
      "<mq:SubmitTRMList>\n" +
      " <mm:trmidList>\n" +
      "  <rdf:Bag>\n" +
      "   <rdf:li>\n" +
      "    <mq:trmTrackPair>\n" +
      "     <mm:trackid>@1@</mm:trackid>\n" +
      "     <mm:trmid>@2@</mm:trmid>\n" +
      "    </mq:trmTrackPair>\n" +
      "   </rdf:li>\n" +
      "  </rdf:Bag>\n" +
      " </mm:trmidList>\n" +
      " <mq:sessionId>@SESSID@</mq:sessionId>\n" +
      " <mq:sessionKey>@SESSKEY@</mq:sessionKey>\n" +
      " <mq:clientVersion>@CLIENTVER@</mq:clientVersion>\n" +
      "</mq:SubmitTRMList>\n",
    MBQ_FileInfoLookup =
      "<mq:FileInfoLookup>\n" +
      "   <mm:trmid>@1@</mm:trmid>\n" +
      "   <mq:artistName>@2@</mq:artistName>\n" +
      "   <mq:albumName>@3@</mq:albumName>\n" +
      "   <mq:trackName>@4@</mq:trackName>\n" +
      "   <mm:trackNum>@5@</mm:trackNum>\n" +
      "   <mm:duration>@6@</mm:duration>\n" +
      "   <mq:fileName>@7@</mq:fileName>\n" +
      "   <mm:artistid>@8@</mm:artistid>\n" +
      "   <mm:albumid>@9@</mm:albumid>\n" +
      "   <mm:trackid>@10@</mm:trackid>\n" +
      "   <mq:maxItems>@MAX_ITEMS@</mq:maxItems>\n" +
      "</mq:FileInfoLookup>\n";
}

}
