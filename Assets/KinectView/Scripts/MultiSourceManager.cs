using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class MultiSourceManager : MonoBehaviour {

    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }

    public uint BytesPerPixel { get; private set; }
    
    private KinectSensor _Sensor;
    private MultiSourceFrameReader _Reader;

    private ushort[] _DepthData;
    private byte[] _ColorData;


    public bool isFresh = false;
    
    public ushort[] GetDepthData()
    {
        return _DepthData;
    }

    public byte[] GetColorData()
    {
        return _ColorData;
    }

    void Start () 
    {
        _Sensor = KinectSensor.GetDefault();
        
        if (_Sensor != null) 
        {
            _Reader = _Sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);
            
            var colorFrameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = colorFrameDesc.Width;
            ColorHeight = colorFrameDesc.Height;
            BytesPerPixel = colorFrameDesc.BytesPerPixel;



            _ColorData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];
            
            var depthFrameDesc = _Sensor.DepthFrameSource.FrameDescription;
            _DepthData = new ushort[depthFrameDesc.LengthInPixels];



            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }

        StartCoroutine(FrameUpdate());

    }
    
    void Update () 
    {

    }


    IEnumerator FrameUpdate()
    {
        do
        {

            if (_Reader != null)
            {
                var frame = _Reader.AcquireLatestFrame();
                if (frame != null)
                {
                    var colorFrame = frame.ColorFrameReference.AcquireFrame();
                    if (colorFrame != null)
                    {

                        colorFrame.CopyConvertedFrameDataToArray(_ColorData, ColorImageFormat.Rgba);
//                        _ColorTexture.LoadRawTextureData(_ColorData);
//                        _ColorTexture.Apply();

                        var depthFrame = frame.DepthFrameReference.AcquireFrame();
                        if (depthFrame != null)
                        {

                            isFresh = true;
                            depthFrame.CopyFrameDataToArray(_DepthData);

                            //CreateDepthTexture(depthFrame);
                            //_DepthTexture.LoadRawTextureData(_DepthDataBytes);
                            //_DepthTexture.Apply();


                            depthFrame.Dispose();
                            depthFrame = null;
      

                        }

                        colorFrame.Dispose();
                        colorFrame = null;
                    }

                    frame = null;
                }
            }
            yield return new WaitForSeconds(0.01f);
        } while (true);

    }
    
    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }
            
            _Sensor = null;
        }
    }
}
