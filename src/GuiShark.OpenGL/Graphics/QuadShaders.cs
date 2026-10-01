namespace GuiShark.OpenGL;

internal static class QuadShaders
{
    public const string Vertex = """
        #version 330 core
        uniform vec4 rect;
        uniform vec2 viewport;
        out vec2 uv;
        const vec2 corners[6] = vec2[](vec2(0,0),vec2(1,0),vec2(1,1),vec2(0,0),vec2(1,1),vec2(0,1));
        void main() {
            uv = corners[gl_VertexID];
            vec2 point = rect.xy + uv * rect.zw;
            gl_Position = vec4(point.x / viewport.x * 2.0 - 1.0, 1.0 - point.y / viewport.y * 2.0, 0, 1);
        }
        """;

    public const string Fragment = """
        #version 330 core
        in vec2 uv;
        out vec4 outputColor;
        uniform vec4 rect;
        uniform vec4 topColor;
        uniform vec4 bottomColor;
        uniform vec4 borderColor;
        uniform float radius;
        uniform float borderWidth;
        uniform bool textured;
        uniform sampler2D image;
        uniform vec4 textureRect;
        float roundedDistance(vec2 p, vec2 halfSize, float r) {
            vec2 q = abs(p) - halfSize + vec2(r);
            return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
        }
        void main() {
            float r = min(radius, min(rect.z, rect.w) * 0.5);
            float d = roundedDistance((uv - 0.5) * rect.zw, rect.zw * 0.5, r);
            float aa = max(fwidth(d), 0.5);
            float coverage = 1.0 - smoothstep(-aa, aa, d);
            if (textured) {
                vec2 sampleUv = textureRect.xy + uv * textureRect.zw;
                vec4 texel = texture(image, sampleUv);
                outputColor = vec4(texel.rgb * topColor.rgb * topColor.a, texel.a * topColor.a) * coverage;
            } else {
                vec4 fill = mix(topColor, bottomColor, uv.y);
                float border = borderWidth > 0.0 ? smoothstep(-borderWidth-aa, -borderWidth+aa, d) : 0.0;
                vec4 color = mix(fill, borderColor, border);
                outputColor = vec4(color.rgb * color.a, color.a) * coverage;
            }
        }
        """;
}
