using Common.Scripts.Timers;
using NUnit.Framework;

namespace Tests.Editor
{
    /// <summary>
    /// SkillCaster의 쿨다운 초기화 로직을 검증한다.
    /// Timer의 OnTimeOutChange 콜백이 _isSkillReady를 덮어쓰는 순서 문제를 포함한다.
    /// </summary>
    public class SkillCasterCooldownResetTests
    {
        private Timer _timer;
        private bool _isSkillReady;

        /// <summary>SkillCaster.SetSkillReady와 동일한 콜백</summary>
        private void SetSkillReady(bool isTimeout, float _)
        {
            _isSkillReady = isTimeout;
        }

        [SetUp]
        public void SetUp()
        {
            _timer = new Timer(5f);
            _timer.OnTimeOutChange += SetSkillReady;
            _isSkillReady = false;
        }

        [TearDown]
        public void TearDown()
        {
            _timer.Dispose();
        }

        // ── ResetCooldown 시나리오 ──

        [Test]
        public void ResetCooldown_MidCooldown_SetsSkillReady()
        {
            // 쿨다운 진행 중 (타이머 실행 중, 아직 타임아웃 아님)
            _timer.Start();
            _timer.Update(2f);
            _isSkillReady = false;

            // ResetCooldown과 동일한 순서: Stop 후 true 설정
            _timer.Stop();
            _isSkillReady = true;

            Assert.IsTrue(_isSkillReady);
            Assert.IsFalse(_timer.IsRunning);
        }

        [Test]
        public void ResetCooldown_WrongOrder_FailsToSetSkillReady()
        {
            // 쿨다운 진행 중
            _timer.Start();
            _timer.Update(2f);
            _isSkillReady = false;

            // 잘못된 순서: true 설정 후 Stop → 콜백이 false로 덮어씀
            _isSkillReady = true;
            _timer.Stop();

            Assert.IsFalse(_isSkillReady);
        }

        // ── OnEnterCombat 시나리오 ──

        [Test]
        public void EnterCombat_CorrectOrder_SetsSkillReady()
        {
            // OnEnterCombat과 동일한 순서: Start 후 true 설정
            _timer.Start();
            _isSkillReady = true;

            Assert.IsTrue(_isSkillReady);
            Assert.IsTrue(_timer.IsRunning);
        }

        [Test]
        public void EnterCombat_WrongOrder_FailsToSetSkillReady()
        {
            // 잘못된 순서: true 설정 후 Start → 콜백이 false로 덮어씀
            _isSkillReady = true;
            _timer.Start();

            Assert.IsFalse(_isSkillReady);
        }

        // ── 타이머 타임아웃 후 ResetCooldown ──

        [Test]
        public void ResetCooldown_AfterTimeout_SkillStaysReady()
        {
            // 타이머가 이미 타임아웃된 상태
            _timer.Start();
            _timer.Update(5f);
            Assert.IsTrue(_isSkillReady);

            // ResetCooldown: 이미 ready이므로 Stop 콜백도 true를 전달
            _timer.Stop();
            _isSkillReady = true;

            Assert.IsTrue(_isSkillReady);
        }
    }
}
