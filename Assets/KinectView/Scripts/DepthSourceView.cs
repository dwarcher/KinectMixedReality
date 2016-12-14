using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class CameraParams
{
    public static float cx = 254.878f;
    public static float cy = 205.395f;
    public static float fx = 365.456f;
    public static float fy = 365.456f;
    public static float k1 = 0.0905474f;
    public static float k2 = -0.26819f;
    public static float k3 = 0.0950862f;
    public static float p1 = 0.0f;
    public static float p2 = 0.0f;
}

public class DepthSourceView : MonoBehaviour
{
    public Material mat;

    public GameObject MultiSourceManager;

    public GameObject headPos;
    public GameObject handPosL;
    public GameObject handPosR;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Mesh _Mesh;
    private Vector3[] _Vertices;
    private Vector3[] _Normals;

    private int[] _Indices;
    private Color[] _Colors;

    public int MaxPoints = 8000;
    public bool calculateNormals = true;
    public bool freeze = false;

    // Only works at 2 or 4 right now
    private int _DownsampleSize = 2;
    private float _DepthScale = 1f;
    public float cheapNormals = 0.8f;
    private const int _Speed = 50;

    float frameRate = 45.0f;
    float frameTimeLeft = 0.0f;

    private MultiSourceManager _MultiManager;


    void Start()
    {


        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            // Downsample to lower resolution
            CreateMesh(frameDesc.Width / _DownsampleSize, frameDesc.Height / _DownsampleSize);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }

        _DepthScale = transform.lossyScale.x;
    }

    void CreateMesh(int width, int height)
    {
        _Mesh = new Mesh();

        GetComponent<MeshFilter>().mesh = _Mesh;

        _Vertices = new Vector3[MaxPoints];
        _Indices = new int[MaxPoints];
        _Colors = new Color[MaxPoints];
        _Normals = new Vector3[MaxPoints];

        for (int i = 0; i < MaxPoints; i++)
        {

            _Vertices[i] = Vector3.zero;
            _Indices[i] = i;
            _Colors[i] = new Color32(255, 0, 255, 255);

        }
        
        _Mesh.vertices = _Vertices;
        _Mesh.colors = _Colors;
        _Mesh.SetIndices(_Indices, MeshTopology.Points, 0);
        _Mesh.normals = _Normals;
        GetComponent<Renderer>().material = mat;

    }

    public void Update()
    {
        if (freeze)
            return;
        if (_Sensor == null)
        {
            return;
        }

        frameTimeLeft -= Time.deltaTime;
        if (frameTimeLeft > 0.0f)
            return;
        frameTimeLeft += 1.0f / frameRate;


        if (MultiSourceManager == null)
        {
            return;
        }

        _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
        if (_MultiManager == null)
        {
            return;
        }

        if (!_MultiManager.isFresh)
        {
            return;
        }

        //gameObject.GetComponent<Renderer>().material.mainTexture = _MultiManager.GetColorTexture();

        RefreshData(_MultiManager.GetDepthData(),
                    _MultiManager.GetColorData());

        _MultiManager.isFresh = false;
    }


    private void RefreshData(ushort[] depthData, byte[] colorData)
    {
        Color32 emptyColor = new Color32(0, 0, 0, 0);
        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;
        //var imageDesc = _Sensor.ColorFrameSource.FrameDescription;
        var imageDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];
        CameraSpacePoint[] cameraSpace = new CameraSpacePoint[depthData.Length];

        _Mapper.MapDepthFrameToCameraSpace(depthData, cameraSpace);
        _Mapper.MapDepthFrameToColorSpace(depthData, colorSpace);

        int stride = (int)imageDesc.BytesPerPixel * imageDesc.Width;

        Bounds boundingBox = new Bounds();

        boundingBox.center = transform.InverseTransformPoint(headPos.transform.position);
        Vector3 _h1 = boundingBox.center;
        Vector3 _h2 = boundingBox.center;

        if (handPosL)
            _h1 = transform.InverseTransformPoint(handPosL.transform.position);

        if (handPosR)
            _h2 = transform.InverseTransformPoint(handPosR.transform.position);




        Vector3 floorPos = headPos.transform.position;
        floorPos.y = 0.1f;


        if (handPosL)
            boundingBox.Encapsulate(_h1);
        if (handPosR)
            boundingBox.Encapsulate(_h2);
        boundingBox.Expand(0.9f / _DepthScale);
        boundingBox.Encapsulate(transform.InverseTransformPoint(floorPos));

        long bigIndexRow = 0;
        long frameWidth = (frameDesc.Width / _DownsampleSize);

        Vector3 hidden = new Vector3(9999.0f, 9999.0f, 9999.0f);

        CameraSpacePoint p;
        int pointIndex = 0;
        uint colorsLength = imageDesc.BytesPerPixel * imageDesc.LengthInPixels - 3;
        //DepthSpacePoint d = new DepthSpacePoint();

        for (int y = 0; y < frameDesc.Height; y += _DownsampleSize)
        {
            if (pointIndex >= MaxPoints)
                break;

            long bigIndex = bigIndexRow;
            Color32 c = new Color32();
            c.a = 255;
            for (int x = 0; x < frameDesc.Width; x += _DownsampleSize)
            {

                if (pointIndex >= MaxPoints)
                    break;

                //d.X = x;
                //d.Y = y;

                p = cameraSpace[bigIndex];

                if (!float.IsNegativeInfinity(p.Z))
                {

                    Vector3 unityPos = new Vector3(-p.X, p.Y, p.Z);
                    var colorSpacePoint = colorSpace[bigIndex];

                    if (colorSpacePoint.X > 0 && colorSpacePoint.Y > 0 && colorSpacePoint.Y < imageDesc.Height && boundingBox.Contains(unityPos)
                        )
                    {
                        _Vertices[pointIndex] = unityPos;

                        long colorI = ((int)colorSpacePoint.X * (int)imageDesc.BytesPerPixel) + ((int)colorSpacePoint.Y * stride);
                        if (colorI < colorsLength)
                        {
                            c.r = colorData[colorI + 0];
                            c.g = colorData[colorI + 1];
                            c.b = colorData[colorI + 2];
     
                            _Colors[pointIndex] = c;

                            _Normals[pointIndex] = (getNormal(depthData, x, y, frameDesc.Width, frameDesc.Height));
                        }

                        pointIndex++;

                    }
                }

                bigIndex += _DownsampleSize;

            }
            bigIndexRow += frameDesc.Width * _DownsampleSize;
        }


        for(int i=pointIndex; i<MaxPoints; i++)
        {
            _Vertices[i] = hidden;
            _Colors[i] = Color.black;
        }


        _Mesh.vertices = _Vertices;
        _Mesh.colors = _Colors;
        _Mesh.normals = _Normals;
        //_Mesh.SetIndices(_Indices, MeshTopology.Points, 0);

        

        _Mesh.RecalculateBounds();


    }

    private Vector3 getNormal1(ushort[] depthData, int x, int y, int width, int height)
    {
        if (x < 1 || x > width - 1)
            return Vector3.zero;
        if (y < 1 || y > height - 1)
            return Vector3.zero;


        float dzdx = ((float)depthData[(y * width) + x + 1] - (float)depthData[(y * width) + x - 1]) / 2f;
        float dzdy = ((float)depthData[((y + 1) * width) + x] - (float)depthData[((y - 1) * width) + x - 1]) / 2f;


        Vector3 d = new Vector3(-dzdx, -dzdy, -1.0f);

        return d.normalized;

    }

    private Vector3 getNormal(ushort[] depthData, int x, int y, int width, int height)
    {
        if (!calculateNormals)
        {
            return -Vector3.forward;
        }

        float h_A = (float)depthData[((y-1) * width) + x];
        float h_B = (float)depthData[(y * width) + x + 1];
        float h_D = (float)depthData[(y * width) + x - 1];
        float h_C = (float)depthData[((y + 1) * width) + x];
        float h_N = (float)depthData[(y * width) + x];
        /*
float h_A = (float)GetAvg(depthData, x, y - 1, width, height);
float h_B = (float)GetAvg(depthData, x + 1, y, width, height);
float h_D = (float)GetAvg(depthData, x - 1, y, width, height);
float h_C = (float)GetAvg(depthData, x, y + 1, width, height);
float h_N = (float)GetAvg(depthData, x, y, width, height);
        */
        Vector3 va = new Vector3(0f, 1f, (h_A - h_N));
        Vector3 vb = new Vector3(1f, 0f, (h_B - h_N));
        Vector3 vc = new Vector3(0f, -1f, (h_C - h_N));
        Vector3 vd = new Vector3(-1f, 0f, (h_D - h_N));
        //cross products of each vector yields the normal of each tri - return the average normal of all 4 tris
        Vector3 average_n = (Vector3.Cross(va, vb) + Vector3.Cross(vb, vc) + Vector3.Cross(vc, vd) + Vector3.Cross(vd, va)) / -4f;
        average_n.z = -average_n.z;
        average_n.y = -average_n.y;
        average_n = Vector3.Normalize(average_n);

        return Vector3.Lerp(average_n, -Vector3.forward, cheapNormals);

    }
    private bool isEdge(ushort[] depthData, int x, int y, int width, int height)
    {
        if (x < 1 || x > width - 1)
            return false;
        if (y < 1 || y > height - 1)
            return false;

        int offRow = ((y - 1) * width);
        for (int y1 = y - 1; y1 < y + 1; y1 += 1)
        {

            for (int x1 = x - 1; x1 < x + 1; x1 += 1)
            {
                int fullIndex = offRow + x1;

                if (depthData[fullIndex] == 0)
                    return true;


            }
            offRow += width;
        }

        return false;
    }


    private float GetAvg(ushort[] depthData, int x, int y, int width, int height)
    {
        float sum = 0.0f;

        if (x < 1 || x >= width - 1)
            return 0.0f;
        if (y < 1 || y >= height - 1)
            return 0.0f;
        
        for (int y1 = y-1; y1 < y + 1; y1++)
        {
            for (int x1 = x-1; x1 < x + 1; x1++)
            {
                int fullIndex = (y1 * width) + x1;
                
                if (depthData[fullIndex] == 0)
                    sum += 4500f;
                else
                    sum += depthData[fullIndex];
                
            }
        }

        return sum / 4;
    }

    Vector3 depthToPointCloudPos(int x, int y, float depthValue)
    {
        Vector3 point = new Vector3();
        point.z = (depthValue);// / (1.0f); // Convert from mm to meters
        point.x = (CameraParams.cx - x) * point.z / CameraParams.fx;
        point.y = (CameraParams.cy - y) * point.z / CameraParams.fy;
        return point;
    }

    void OnApplicationQuit()
    {
        if (_Mapper != null)
        {
            _Mapper = null;
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
