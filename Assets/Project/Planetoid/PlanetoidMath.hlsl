
#include <UnityShaderVariables.cginc>
#ifndef _PLANETOID_MATH
#define _PLANETOID_MATH

SamplerState sampler_linear_clamp
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

#define PI 3.14159265

uint3 Dims;
float4x4 Offset;
float4x4 OffsetIT;

float NoiseFrequency;
float NoiseScale;
float Radius;
float HeightDelta;
float Time;
float WaterThreshold;

Texture2D HeightMapTexture;


float Hash (float3 position)
{
    float3 magic = float3(37.0, 17.0, 29.0);
    return frac(sin(dot(position, magic)) * 143758.5453);
}
float Noise (float3 position)
{
    float3 floorPos = floor(position);
    float3 fraction = position - floorPos;

    // Smooth the fraction with fade function.
    fraction = fraction * fraction * (3.0 - 2.0 * fraction);

    // Hash coordinates of the 8 cube corners.
    float3 offsets[8] = {
        float3(-0.5, -0.5, -0.5),
        float3( 0.5, -0.5, -0.5),
        float3(-0.5,  0.5, -0.5),
        float3( 0.5,  0.5, -0.5),
        float3(-0.5, -0.5,  0.5),
        float3( 0.5, -0.5,  0.5),
        float3(-0.5,  0.5,  0.5),
        float3( 0.5,  0.5,  0.5)
    };

    float values[8];
    
    for (int i = 0; i < 8; i++)
    {
        float3 offset = offsets[i];
        float3 gridPos = floorPos + offset;
        float value = Hash(gridPos);
        values[i] = value;
    }

    // Interpolate between the values.
    float value = lerp(lerp(lerp(values[0], values[1], fraction.x), lerp(values[2], values[3], fraction.x), fraction.y), lerp(lerp(values[4], values[5], fraction.x), lerp(values[6], values[7], fraction.x), fraction.y), fraction.z);
    return value;
}

// float SphereSurfaceVisible(float normalisedDistance) {
//     // Convert normalised distance back to actual distance
//     float distance = normalisedDistance * Radius;
//
//     // Compute the angle of visibility using trigonometry
//     float visibleAngle = 2.0 * atan(radius / distance);
//
//     // Clamp the angle to a maximum of 180 degrees (in radians)
//     return min(visibleAngle, 3.14159265358979323846);
// }


float fmap(float value, float inMin, float inMax, float outMin, float outMax)
{
    return outMin + ((value - inMin) / (inMax - inMin)) * (outMax - outMin);
}

float3 fmap(float3 value, float3 inMin, float3 inMax, float3 outMin, float3 outMax)
{
    return float3(fmap(value.x, inMin.x, inMax.x, outMin.x, outMax.x),
        fmap(value.y, inMin.y, inMax.y, outMin.y, outMax.y),
        fmap(value.z, inMin.z, inMax.z, outMin.z, outMax.z));
}

float MakeDepthMoreLinear(float zNDC)
{
    // First, normalize zNDC from [-1,1] to [0,1]
    float zNormalized = (zNDC + 1) / 2;
    
    float multi = 0.1;
    // Then, apply the square root function to make the depth more linear
    // This function will distribute values more evenly when original values are biased towards 1
    float zLinear = pow(abs(zNormalized), multi);
    
    float nearClipPLane = 0.99;
    
    float zOffset = pow(nearClipPLane, multi) - nearClipPLane;
    // zLinear += zOffset;
    
    // Then, convert it back to [-1,1]
    float zLinearNDC = zLinear * 2 - 1;

    return zLinearNDC;
}
float2 SpherePosToUV(float3 pos)
{
    // Normalize the position vector
    float3 norm_pos = normalize(pos);

    // Convert the 3D vector to spherical coordinates (latitude and longitude)
    float longitude = atan2(norm_pos.z, norm_pos.x);
    float latitude = asin(norm_pos.y);

    // Normalize the spherical coordinates to the range [0, 1]
    float u = (longitude + PI) / (2.0 * PI);
    float v = (latitude + PI / 2.0) / PI;

    return float2(u, v);
}
float HeightAtPos(float3 pos)
{
    // Normalize the position vector
    float3 norm_pos = normalize(pos);

    // Convert the 3D vector to spherical coordinates (latitude and longitude)
    float longitude = atan2(norm_pos.z, norm_pos.x);
    float latitude = asin(norm_pos.y);

    // Normalize the spherical coordinates to the range [0, 1]
    float u = (longitude + PI) / (2.0 * PI);
    float v = (latitude + PI / 2.0) / PI;

    int textureWidth, textureHeight;
    HeightMapTexture.GetDimensions(textureWidth, textureHeight);

    // Convert texture coordinates to pixel coordinates
    float uf = u * textureWidth;
    float vf = v * textureHeight;

    // Separate into integer and fractional parts
    int ui = (int)uf;
    int vi = (int)vf;
    float ufrac = uf - ui;
    float vfrac = vf - vi;

    // Fetch the height values from the four neighboring pixels
    float h00 = HeightMapTexture[clamp(int2(ui, vi), 0, int2(textureWidth - 1, textureHeight - 1))].r;
    float h10 = HeightMapTexture[clamp(int2(ui + 1, vi), 0, int2(textureWidth - 1, textureHeight - 1))].r;
    float h01 = HeightMapTexture[clamp(int2(ui, vi + 1), 0, int2(textureWidth - 1, textureHeight - 1))].r;
    float h11 = HeightMapTexture[clamp(int2(ui + 1, vi + 1), 0, int2(textureWidth - 1, textureHeight - 1))].r;

    // Perform bilinear interpolation
    float height = lerp(lerp(h00, h10, ufrac), lerp(h01, h11, ufrac), vfrac);
    return pow(height, 1)*10 + Radius;
    float relevantEnd = 0.05;
    
    bool land = height > WaterThreshold;

    float heightFromWater = height - WaterThreshold;
    // if (land)
    // {
        heightFromWater = pow(heightFromWater, relevantEnd);
        heightFromWater = fmap(heightFromWater, 0.7, 1, 0, 1);//clamp(fmap(height, , -100, 100);
    // }
    height = WaterThreshold + heightFromWater;
    

    float finalHeight = Radius + ((height) * HeightDelta);

    
    if (!land)
        finalHeight -= 0.1;//pow(height, 1.5);
    return finalHeight;
}


float ValueAtPont (float3 pos)
{
    float distanceFromCenter = length(pos);
    float heightAtPoint = HeightAtPos(pos);
    float normalisedDistanceFromCenter = distanceFromCenter/heightAtPoint;
    
    // float value = abs(frac((float)id.z/2));// normalisedDistanceFromCenter;
        
    // float value = pow(1-distance(pos, Dims/2)/Dims.x + sin(Time / 10)/10, 6)*12;
    float value = 0;

    value += normalisedDistanceFromCenter;
    
    // float distanceScale = (1/fmap(normPos.z, -1, 1, 0, 1));
    float noise = Noise(pos * NoiseFrequency / 30) * NoiseScale;
    
    if (noise > 0.999)
    {
        value += noise - 0.999;
    }
    // value -= clamp(sin(pos.x + pos.y + pos.y)/20 * NoiseScale, 0, 1);
    // value += sin(clamp(pos.y/100, 0, 1));
    // value -= clamp(Noise((pos q * NoiseFrequency ) * 0.2) * NoiseScale, 0, 1);
    // value += clamp(Noise((pos * NoiseFrequency - float3(Time,Time,Time)/32)*4) * NoiseScale/2, 0, 1);
    // value += clamp(Noise(pos * NoiseFrequency*32) * NoiseScale/32, 0, 1);
    // value += clamp(Noise(pos * NoiseFrequency*64) * NoiseScale/64, 0, 1);


    // value = fmap(sin(id.z), -1, 1, 0, 1);

    return value;
}

float3 ScreenToWorldPoint (float3 screenPoint)
{
    screenPoint.z = MakeDepthMoreLinear(screenPoint.z);
    
    float4 multiPos = mul(Offset, float4(screenPoint.xyz, 1));
    return multiPos.xyz / multiPos.w;
}

// float3 ScreenToWorldDirection (float3 normal)
// {
    // float3x3 normalMatrix = transpose(inverse(float3x3(Offset._11_12_13, Offset._21_22_23, Offset._31_32_33)));
    // float3 worldNormal = mul(normalMatrix, normal);

    // float3x3 normalMatrix = (float3x3)UNITY_MATRIX_IT_MV; 
    // float3 worldNormal = mul(normalMatrix, normal);
    // float3x3 rotationScaleMatrix = (float3x3) Offset;
    // float3 worldNormal = mul(rotationScaleMatrix, normal);
    
//     float3x3 normalMatrix = transpose(float3x3(OffsetI._11_12_13, OffsetI._21_22_23, OffsetI._31_32_33));
//     float3 worldNormal = mul(normalMatrix, normal);
//
//
//     return normal;
// }
float3 ScreenToWorldDirection(float3 normal)
{
    // Calculate the inverse transpose of the Offset matrix
    float4x4 OffsetInverseTranspose = OffsetIT;
    
    // Transform the normal
    float4 worldNormal = mul(OffsetInverseTranspose, float4(normal, 0));
    
    // Normalize the result to ensure it's still a unit vector
    return normalize(worldNormal.xyz);
}

float3 GridPointToWorldPoint(uint3 gridPoint)
{
    return ScreenToWorldPoint(fmap(gridPoint,float3(0,0,0), Dims, float3(-1,-1,-1),float3(1,1,1)));
}

float ValueAtGridPoint(uint3 gridPoint)
{
    // return gridPoint.z % 10;
    float3 worldPos = GridPointToWorldPoint(gridPoint);
    return ValueAtPont(worldPos) + sin(worldPos.y)/50 + clamp(sin(tan(worldPos.x)) / 100, 0, 100) + cos(worldPos.z)/50;
}

float ValueAtScreenPoint (float3 pos)
{
    return ValueAtPont(ScreenToWorldPoint(pos));
}

#endif
