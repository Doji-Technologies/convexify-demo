using System;

public enum MimeType {

    [MimeValue("")]
    Unknown,

    [MimeValue("text/css")]
    TextCss,

    [MimeValue("text/html")]
    TextHtml,

    [MimeValue("text/javascript")]
    TextJavaScript,

    [MimeValue("text/plain")]
    TextPlain,

    [MimeValue("text/xml")]
    TextXml,

    [MimeValue("application/json")]
    ApplicationJson,

    [MimeValue("application/octet-stream")]
    ApplicationOctetStream,

    [MimeValue("application/pdf")]
    ApplicationPdf,

    [MimeValue("application/xml")]
    ApplicationXml,

    [MimeValue("application/xhtml+xml")]
    ApplicationXhtmlXml,

    [MimeValue("image/gif")]
    ImageGif,

    [MimeValue("image/jpeg")]
    ImageJpeg,

    [MimeValue("image/ktx")]
    ImageKtx,

    [MimeValue("image/ktx2")]
    ImageKtx2,

    [MimeValue("image/png")]
    ImagePng,

    [MimeValue("image/tiff")]
    ImageTiff,

    [MimeValue("audio/mpeg")]
    AudioMpeg,

    [MimeValue("audio/wav")]
    AudioWav,

    [MimeValue("video/mp4")]
    VideoMp4,

    [MimeValue("video/mpeg")]
    VideoMpeg,
}

public static class MimeTypeExtensions {
    public static string ToMimeString(this MimeType mimeType) {
        var type = mimeType.GetType();
        var fieldInfo = type.GetField(mimeType.ToString());
        var attributes = fieldInfo.GetCustomAttributes(typeof(MimeValueAttribute), false) as MimeValueAttribute[];

        if (attributes != null && attributes.Length > 0) {
            return attributes[0].Value;
        }

        return null;
    }
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
sealed class MimeValueAttribute : Attribute {
    public string Value { get; }

    public MimeValueAttribute(string value) {
        Value = value;
    }
}