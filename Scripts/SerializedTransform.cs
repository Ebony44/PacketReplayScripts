using UnityEngine;
using System;

[System.Serializable]
public class SerializedTransform
{
    public float[] _position = new float[3];
    public float[] _rotation = new float[4];
    public float[] _scale = new float[3];


    public SerializedTransform(Transform transform, bool worldSpace = false)
    {
        _position[0] = transform.localPosition.x;
        _position[1] = transform.localPosition.y;
        _position[2] = transform.localPosition.z;

        _rotation[0] = transform.localRotation.w;
        _rotation[1] = transform.localRotation.x;
        _rotation[2] = transform.localRotation.y;
        _rotation[3] = transform.localRotation.z;

        _scale[0] = transform.localScale.x;
        _scale[1] = transform.localScale.y;
        _scale[2] = transform.localScale.z;

    }
    public static implicit operator SerializedTransform(Transform transform)
    {
        var result = new SerializedTransform(transform);
        return result;
    }


}

public static class SerializedTransformExtention
{
    public static void DeserialTransform(this SerializedTransform _serializedTransform, Transform _transform)
    {
        _transform.localPosition = new Vector3(_serializedTransform._position[0], _serializedTransform._position[1], _serializedTransform._position[2]);
        _transform.localRotation = new Quaternion(_serializedTransform._rotation[1], _serializedTransform._rotation[2], _serializedTransform._rotation[3], _serializedTransform._rotation[0]);
        _transform.localScale = new Vector3(_serializedTransform._scale[0], _serializedTransform._scale[1], _serializedTransform._scale[2]);
    }
}

public static class TransformExtention
{
    public static void SetTransformEX(this Transform original, Transform copy)
    {
        original.position = copy.position;
        original.rotation = copy.rotation;
        original.localScale = copy.localScale;
    }
}

[System.Serializable]
public struct SerializableVector3
{
    /// <summary>
    /// x component
    /// </summary>
    public float x;

    /// <summary>
    /// y component
    /// </summary>
    public float y;

    /// <summary>
    /// z component
    /// </summary>
    public float z;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rX"></param>
    /// <param name="rY"></param>
    /// <param name="rZ"></param>
    public SerializableVector3(float rX, float rY, float rZ)
    {
        x = rX;
        y = rY;
        z = rZ;
    }

    /// <summary>
    /// Returns a string representation of the object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return String.Format("[{0}, {1}, {2}]", x, y, z);
    }

    /// <summary>
    /// Automatic conversion from SerializableVector3 to Vector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator Vector3(SerializableVector3 rValue)
    {
        return new Vector3(rValue.x, rValue.y, rValue.z);
    }

    /// <summary>
    /// Automatic conversion from Vector3 to SerializableVector3
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator SerializableVector3(Vector3 rValue)
    {
        return new SerializableVector3(rValue.x, rValue.y, rValue.z);
    }
}