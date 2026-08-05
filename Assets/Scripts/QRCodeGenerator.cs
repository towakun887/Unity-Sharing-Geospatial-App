using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing;
using ZXing.QrCode;

public class QRCodeGenerator : MonoBehaviour
{
    public RawImage qrCodeImage;
    public TextMeshProUGUI debugText;
    
    public string DeviceId { get; private set; }

    public string ConnectionString { get; private set; }

    private void Awake()
    {
        // Generate random 12 char alphanumeric ID
        DeviceId = GenerateRandomID(12);
    }

    public void GenerateAndShowQRCode(int port)
    {
        string ip = TcpServerController.GetLocalIPAddress();
        ConnectionString = $"{ip}:{port},ID:{DeviceId}";

        Texture2D texture = GenerateQRTexture(ConnectionString, 512, 512);
        if (texture != null)
        {
            qrCodeImage.texture = texture;
            qrCodeImage.gameObject.SetActive(true);
        }
    }

    private Texture2D GenerateQRTexture(string text, int width, int height)
    {
        try
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = 1
                }
            };

            var color32s = writer.Write(text);
            var texture = new Texture2D(width, height);
            texture.SetPixels32(color32s);
            texture.Apply();
            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating QR Code: {ex.Message}");
            return null;
        }
    }

    private string GenerateRandomID(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        return new string(result);
    }
}
