
#ifndef _PLANETOID_MATH
#define _PLANETOID_MATH

uint3 Dims;
float4x4 Offset;

float NoiseFrequency;
float NoiseScale;
float Radius;
float Time;


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

    // Then, apply the square root function to make the depth more linear
    // This function will distribute values more evenly when original values are biased towards 1
    float zLinear = pow(abs(zNormalized), 0.1);

    // Then, convert it back to [-1,1]
    float zLinearNDC = zLinear * 2 - 1;

    return zLinearNDC;
}

float ValueAtPont (float3 pos)
{
    float distanceFromCenter = length(pos);
    float normalisedDistanceFromCenter = Radius / distanceFromCenter;
    
    // float value = abs(frac((float)id.z/2));// normalisedDistanceFromCenter;
        
    // float value = pow(1-distance(pos, Dims/2)/Dims.x + sin(Time / 10)/10, 6)*12;
    float value = 0;

    value += normalisedDistanceFromCenter;

    // float distanceScale = (1/fmap(normPos.z, -1, 1, 0, 1));
    value -= clamp(Noise(pos * NoiseFrequency / 30) * 0.1, 0, 1);
    // value -= clamp(Noise(pos * NoiseFrequency ) * NoiseScale, 0, 1);
    // value -= clamp(Noise((pos * NoiseFrequency ) * 0.2) * NoiseScale, 0, 1);
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

float3 GridPointToWorldPoint(uint3 gridPoint)
{
    return fmap(gridPoint,float3(0,0,0), Dims, float3(-1,-1,-1),float3(1,1,1));
}

float ValueAtGridPoint(uint3 gridPoint)
{
    return ValueAtPont(GridPointToWorldPoint(gridPoint));
}

float ValueAtScreenPoint (float3 pos)
{
    return ValueAtPont(ScreenToWorldPoint(pos));
}

#endif
