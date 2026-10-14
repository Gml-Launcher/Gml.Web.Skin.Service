using Gml.Web.Skin.Service.Models;
using SkiaSharp;

namespace Gml.Web.Skin.Service.Core.SkinViewer;

public abstract class SkinViewer
{
    private static readonly SKSamplingOptions NearestSampling = new(SKFilterMode.Nearest, SKMipmapMode.None);

    public static byte[] GetHead(string skinPath, UserTexture userTexture, int size)
    {
        if (!File.Exists(skinPath))
        {
            return [];
        }

        using var inputImage = SKBitmap.Decode(skinPath);
        if (inputImage == null)
        {
            return [];
        }

        var scaleFactor = Math.Max(1, inputImage.Width / 64);

        using var croppedImage = Crop(inputImage, 8 * scaleFactor, 8 * scaleFactor, 8 * scaleFactor, 8 * scaleFactor);

        return ResizeAndEncode(size, croppedImage);
    }

    private static int GetScaleSize(double size, int croppedImageWidth)
    {
        return Convert.ToInt32(Math.Round(size / croppedImageWidth));
    }

    public static byte[] GetCloak(string cloakPath, UserTexture user, int size)
    {
        if (!File.Exists(cloakPath))
        {
            return [];
        }

        using var inputImage = SKBitmap.Decode(cloakPath);
        if (inputImage == null)
        {
            return [];
        }

        var scaleFactor = Math.Max(1, inputImage.Width / 64);

        using var croppedImage = Crop(inputImage, 0, 0, 11 * scaleFactor, 17 * scaleFactor);

        return ResizeAndEncode(size, croppedImage);
    }

    public static byte[] GetFront(string skinPath, UserTexture user, int size)
    {
        if (!File.Exists(skinPath))
        {
            return [];
        }

        using var inputImage = SKBitmap.Decode(skinPath);
        if (inputImage == null)
        {
            return [];
        }

        var scaleFactor = Math.Max(1, inputImage.Width / 64);
        var extendedSkin = inputImage.Height / scaleFactor >= 64;
        var isSlim = user.SkinType == SkinType.Slim;

        using var croppedHead = Crop(inputImage, 8 * scaleFactor, 8 * scaleFactor, 8 * scaleFactor, 8 * scaleFactor);
        using var croppedBody = Crop(inputImage, 20 * scaleFactor, 20 * scaleFactor, 8 * scaleFactor, 12 * scaleFactor);
        using var leftCroppedLeg = Crop(inputImage, 4 * scaleFactor, 20 * scaleFactor, 4 * scaleFactor, 12 * scaleFactor);

        // Для slim скина рука тоньше на 1 пиксель
        var armWidth = isSlim ? 3 * scaleFactor : 4 * scaleFactor;
        using var leftCroppedArm = Crop(inputImage, 44 * scaleFactor, 20 * scaleFactor, armWidth, 12 * scaleFactor);

        using var rightCroppedLeg = extendedSkin
            ? Crop(inputImage, 20 * scaleFactor, 52 * scaleFactor, 4 * scaleFactor, 12 * scaleFactor)
            : FlipHorizontal(leftCroppedLeg);

        using var rightCroppedArm = extendedSkin
            ? Crop(inputImage, 36 * scaleFactor, 52 * scaleFactor, armWidth, 12 * scaleFactor)
            : FlipHorizontal(leftCroppedArm);

        using var secondLayerHead = Crop(inputImage, 40 * scaleFactor, 8 * scaleFactor, 8 * scaleFactor, 8 * scaleFactor);

        var newWidth = (leftCroppedArm.Width * 2) + croppedBody.Width;
        var newHeight = croppedHead.Height + croppedBody.Height + leftCroppedArm.Height;

        using var combinedImage = new SKBitmap(newWidth, newHeight);

        using (var canvas = new SKCanvas(combinedImage))
        {
            canvas.Clear(SKColors.Transparent);

            var headX = (newWidth - croppedHead.Width) / 2;
            var headY = 0;

            var bodyX = headX;
            var bodyY = croppedHead.Height;

            var leftArmX = 0;
            var leftArmY = bodyY;

            var rightArmX = leftCroppedArm.Width + croppedBody.Width;
            var rightArmY = leftArmY;

            var leftLegX = leftCroppedArm.Width;
            var leftLegY = croppedHead.Height + croppedBody.Height;

            var rightLegX = leftCroppedArm.Width + leftCroppedLeg.Width;
            var rightLegY = leftLegY;

            canvas.DrawBitmap(croppedHead, headX, headY, NearestSampling);
            canvas.DrawBitmap(croppedBody, bodyX, bodyY, NearestSampling);
            canvas.DrawBitmap(leftCroppedArm, leftArmX, leftArmY, NearestSampling);
            canvas.DrawBitmap(rightCroppedArm, rightArmX, rightArmY, NearestSampling);
            canvas.DrawBitmap(leftCroppedLeg, leftLegX, leftLegY, NearestSampling);
            canvas.DrawBitmap(rightCroppedLeg, rightLegX, rightLegY, NearestSampling);

            canvas.DrawBitmap(secondLayerHead, headX, headY, NearestSampling);
        }

        return ResizeAndEncode(size, combinedImage);
    }

    public static byte[] GetBack(UserTexture user, int size, bool includeCloak = false)
    {
        if (!File.Exists(user.SkinFullPath))
        {
            return [];
        }

        using var inputImage = SKBitmap.Decode(user.SkinFullPath);
        if (inputImage == null)
        {
            return [];
        }

        using var cloakImage = includeCloak && user.HasCloak && File.Exists(user.CloakFullPath)
            ? SKBitmap.Decode(user.CloakFullPath)
            : null;

        var skinScaleFactor = Math.Max(1, inputImage.Width / 64);
        var cloakScaleFactor = cloakImage?.Width >= 64 ? cloakImage.Width / 64 : 1;
        var needSkinResizeToCloakSize = cloakScaleFactor > skinScaleFactor;
        var extendedSkin = inputImage.Height / skinScaleFactor >= 64;
        var isSlim = user.SkinType == SkinType.Slim;

        var croppedHead = Crop(inputImage, 24 * skinScaleFactor, 8 * skinScaleFactor, 8 * skinScaleFactor, 8 * skinScaleFactor);
        var croppedBody = Crop(inputImage, 32 * skinScaleFactor, 20 * skinScaleFactor, 8 * skinScaleFactor, 12 * skinScaleFactor);
        var croppedRightLeg = Crop(inputImage, 12 * skinScaleFactor, 20 * skinScaleFactor, 4 * skinScaleFactor, 12 * skinScaleFactor);

        // Для slim скина рука тоньше на 1 пиксель
        var armWidth = isSlim ? 3 * skinScaleFactor : 4 * skinScaleFactor;
        var croppedRightArm = Crop(inputImage, 52 * skinScaleFactor, 20 * skinScaleFactor, armWidth, 12 * skinScaleFactor);

        var croppedLeftLeg = extendedSkin
            ? Crop(inputImage, 28 * skinScaleFactor, 52 * skinScaleFactor, 4 * skinScaleFactor, 12 * skinScaleFactor)
            : FlipHorizontal(croppedRightLeg);

        var croppedLeftArm = extendedSkin
            ? Crop(inputImage, 44 * skinScaleFactor, 52 * skinScaleFactor, armWidth, 12 * skinScaleFactor)
            : FlipHorizontal(croppedRightArm);

        if (needSkinResizeToCloakSize)
        {
            croppedHead = ReplaceWithResized(croppedHead, cloakScaleFactor);
            croppedBody = ReplaceWithResized(croppedBody, cloakScaleFactor);
            croppedLeftLeg = ReplaceWithResized(croppedLeftLeg, cloakScaleFactor);
            croppedRightLeg = ReplaceWithResized(croppedRightLeg, cloakScaleFactor);
            croppedLeftArm = ReplaceWithResized(croppedLeftArm, cloakScaleFactor);
            croppedRightArm = ReplaceWithResized(croppedRightArm, cloakScaleFactor);
        }

        SKBitmap? croppedCloak = null;
        if (includeCloak && cloakImage != null)
        {
            using var rawCloak = Crop(cloakImage, 0, 0, 11 * cloakScaleFactor, 17 * cloakScaleFactor);
            croppedCloak = ResizeImage(rawCloak, croppedBody.Width, croppedBody.Height);
        }

        try
        {
            var newWidth = croppedLeftArm.Width * 2 + croppedBody.Width;
            var newHeight = croppedHead.Height + croppedBody.Height + croppedLeftArm.Height;

            using var combinedImage = new SKBitmap(newWidth, newHeight);

            using (var canvas = new SKCanvas(combinedImage))
            {
                canvas.Clear(SKColors.Transparent);

                var headX = (newWidth - croppedHead.Width) / 2;
                var headY = 0;

                var bodyX = headX;
                var bodyY = croppedHead.Height;

                var leftArmX = 0;
                var leftArmY = bodyY;

                var rightArmX = croppedLeftArm.Width + croppedBody.Width;
                var rightArmY = leftArmY;

                var leftLegX = croppedLeftArm.Width;
                var leftLegY = croppedHead.Height + croppedBody.Height;

                var rightLegX = croppedLeftArm.Width + croppedLeftLeg.Width;
                var rightLegY = leftLegY;

                canvas.DrawBitmap(croppedHead, headX, headY, NearestSampling);
                canvas.DrawBitmap(croppedBody, bodyX, bodyY, NearestSampling);
                canvas.DrawBitmap(croppedLeftArm, leftArmX, leftArmY, NearestSampling);
                canvas.DrawBitmap(croppedRightArm, rightArmX, rightArmY, NearestSampling);
                canvas.DrawBitmap(croppedLeftLeg, leftLegX, leftLegY, NearestSampling);
                canvas.DrawBitmap(croppedRightLeg, rightLegX, rightLegY, NearestSampling);

                if (croppedCloak != null)
                {
                    canvas.DrawBitmap(croppedCloak, bodyX, bodyY, NearestSampling);
                }
            }

            return ResizeAndEncode(size, combinedImage);
        }
        finally
        {
            croppedHead.Dispose();
            croppedBody.Dispose();
            croppedRightLeg.Dispose();
            croppedRightArm.Dispose();
            croppedLeftLeg.Dispose();
            croppedLeftArm.Dispose();
            croppedCloak?.Dispose();
        }
    }

    private static SKBitmap ReplaceWithResized(SKBitmap source, int scaleFactor)
    {
        var resized = ResizeImage(source, source.Width * scaleFactor, source.Height * scaleFactor);
        source.Dispose();
        return resized;
    }

    private static SKBitmap Crop(SKBitmap source, int x, int y, int width, int height)
    {
        var cropped = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(cropped);
        canvas.Clear(SKColors.Transparent);
        var srcRect = SKRectI.Create(x, y, width, height);
        var destRect = SKRect.Create(0, 0, width, height);
        canvas.DrawBitmap(source, srcRect, destRect, NearestSampling);
        return cropped;
    }

    private static SKBitmap FlipHorizontal(SKBitmap source)
    {
        var flipped = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(flipped);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(-1, 1);
        canvas.Translate(-source.Width, 0);
        canvas.DrawBitmap(source, 0, 0, NearestSampling);
        return flipped;
    }

    private static SKBitmap ResizeImage(SKBitmap source, int newWidth, int newHeight)
    {
        var resized = new SKBitmap(newWidth, newHeight, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(resized);
        canvas.Clear(SKColors.Transparent);
        var destRect = SKRect.Create(0, 0, newWidth, newHeight);
        canvas.DrawBitmap(source, destRect, NearestSampling);
        return resized;
    }

    private static byte[] ResizeAndEncode(int size, SKBitmap image)
    {
        if (size != image.Width)
        {
            var scaleSize = GetScaleSize(size, image.Width);
            if (scaleSize != 0)
            {
                using var resized = ResizeImage(image, image.Width * scaleSize, image.Height * scaleSize);
                return EncodeToPng(resized);
            }
        }

        return EncodeToPng(image);
    }

    private static byte[] EncodeToPng(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
