using Media.API.Contracts;
using Media.API.Controllers;
using Media.Application.Contracts.Repositories;
using Media.API.Services;
using Media.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Media.Test.Share;

public class ShareRuleControllerTests
{
  private readonly Mock<IShareRuleRepository> repository = new();
  private readonly Mock<IShareLinkRevocationQueue> revocation = new();

  [Fact]
  public async Task Save_RejectsRuleWithoutCriterion()
  {
    var controller = new ShareRuleController(repository.Object, revocation.Object);

    Assert.IsType<BadRequestResult>(await controller.Save(new ShareRuleRequest { Groups = [Guid.NewGuid()] }));
    repository.Verify(r => r.Save(It.IsAny<Guid?>(), It.IsAny<IList<Guid>>(), It.IsAny<bool>(), It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>()), Times.Never);
  }

  [Fact]
  public async Task Save_CreatesOrChangesRule()
  {
    var id = Guid.NewGuid();
    var group = Guid.NewGuid();
    var album = Guid.NewGuid();
    repository.Setup(r => r.Save(id, It.IsAny<IList<Guid>>(), false, It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>())).ReturnsAsync(true);
    repository.Setup(r => r.Save(null, It.IsAny<IList<Guid>>(), It.IsAny<bool>(), It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>())).ReturnsAsync(true);
    var controller = new ShareRuleController(repository.Object, revocation.Object);

    Assert.IsType<OkResult>(await controller.Save(new ShareRuleRequest { Groups = [group], Albums = [album] }));
    Assert.IsType<OkResult>(await controller.Save(new ShareRuleRequest { Id = id, Groups = [group], Albums = [album] }));
    repository.Verify(r => r.Save(null, new List<Guid> { group }, false, It.IsAny<IList<Guid>>(), new List<Guid> { album }, It.IsAny<IList<Guid>>()), Times.Once);
    revocation.Verify(r => r.Enqueue(), Times.Exactly(2));
  }

  [Fact]
  public async Task Save_AllowsRuleForAllRecordsWithoutCriterion()
  {
    var group = Guid.NewGuid();
    repository.Setup(r => r.Save(null, It.IsAny<IList<Guid>>(), true, It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>())).ReturnsAsync(true);
    var controller = new ShareRuleController(repository.Object, revocation.Object);

    Assert.IsType<OkResult>(await controller.Save(new ShareRuleRequest { Groups = [group], AllRecords = true }));
    repository.Verify(r => r.Save(null, new List<Guid> { group }, true, It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>(), It.IsAny<IList<Guid>>()), Times.Once);
    revocation.Verify(r => r.Enqueue(), Times.Once);
  }

  [Fact]
  public async Task Save_ReportsUnknownRule()
  {
    var controller = new ShareRuleController(repository.Object, revocation.Object);

    var result = await controller.Save(new ShareRuleRequest { Id = Guid.NewGuid(), Groups = [Guid.NewGuid()], Genres = [Guid.NewGuid()] });

    Assert.IsType<NotFoundResult>(result);
    revocation.Verify(r => r.Enqueue(), Times.Never);
  }

  [Fact]
  public void Get_ReportsUnknownRule()
  {
    var known = new ShareRule { Id = Guid.NewGuid() };
    repository.Setup(r => r.Load(known.Id)).Returns(known);
    var controller = new ShareRuleController(repository.Object, revocation.Object);

    Assert.Same(known, controller.Get(known.Id).Value);
    Assert.IsType<NotFoundResult>(controller.Get(Guid.NewGuid()).Result);
  }

  [Fact]
  public async Task DeleteList_DeletesRules()
  {
    var ids = new List<Guid> { Guid.NewGuid() };

    Assert.IsType<OkResult>(await new ShareRuleController(repository.Object, revocation.Object).DeleteList(ids));

    repository.Verify(r => r.Delete(ids), Times.Once);
    revocation.Verify(r => r.Enqueue(), Times.Once);
  }
}
