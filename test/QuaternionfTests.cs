using System;
using FluentAssertions;
using g3;
using Xunit;

namespace geometry3sharp.Tests;

public class QuaternionfTests
{
    private const float Precision = 1e-5f;
    private static readonly Random Random = new((int)DateTime.UtcNow.Ticks);

    [Theory]
    [InlineData(120)]
    [InlineData(60)]
    [InlineData(30)]
    [InlineData(15)]
    public void ShouldCalculateAngularDistance(float degrees) =>
        Quaternionf.Identity.AngularDistanceR(Quaternionf.AxisAngleD(Vector3f.AxisX, angleDeg: degrees)).Should()
            .BeApproximately(expectedValue: degrees * MathUtil.Deg2Radf, precision: Precision);

    [Fact]
    public void ShouldReturnIdentity_WhenMultipliedByConjugate()
    {
        Quaternionf q = new Quaternionf(Random.Next(), Random.Next(), Random.Next(), Random.Next()).Normalized;
        Quaternionf product = q * q.Conjugate();
        product.x.Should().BeApproximately(0, Precision);
        product.y.Should().BeApproximately(0, Precision);
        product.z.Should().BeApproximately(0, Precision);
        product.w.Should().BeApproximately(1, Precision);
    }
}