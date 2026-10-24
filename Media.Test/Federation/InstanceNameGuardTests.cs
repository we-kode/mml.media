using Media.Application.Contracts.Repositories;
using Media.Infrastructure.Services;
using Moq;
using System;
using Xunit;

namespace Media.Test.Federation;

public class InstanceNameGuardTests
{
  private readonly Mock<ISettingRepository> settings = new();

  private void Stored(string name) =>
    settings.Setup(s => s.Get(InstanceNameGuard.SettingKey, string.Empty)).Returns(name);

  [Fact]
  public void Ensure_StoresNameAtFirstStart()
  {
    Stored(string.Empty);

    InstanceNameGuard.Ensure(settings.Object, "kst.dev");

    settings.Verify(s => s.Save(InstanceNameGuard.SettingKey, "kst.dev"), Times.Once);
  }

  [Fact]
  public void Ensure_AcceptsUnchangedName()
  {
    Stored("kst.dev");

    InstanceNameGuard.Ensure(settings.Object, "kst.dev");

    settings.Verify(s => s.Save(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Theory]
  [InlineData("ecg.test")]
  [InlineData("KST.dev")]
  public void Ensure_RejectsChangedName(string configured)
  {
    Stored("kst.dev");

    var exception = Assert.Throws<InvalidOperationException>(() => InstanceNameGuard.Ensure(settings.Object, configured));

    Assert.Contains($"'{configured}'", exception.Message);
    Assert.Contains("'kst.dev'", exception.Message);
    settings.Verify(s => s.Save(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }
}
