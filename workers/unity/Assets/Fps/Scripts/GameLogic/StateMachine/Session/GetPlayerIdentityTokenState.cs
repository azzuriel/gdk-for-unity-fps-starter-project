using System.Diagnostics;
using Improbable.Gdk.Core;
using Improbable.Worker.CInterop;
using Improbable.Worker.CInterop.Alpha;

namespace Fps
{
    public class GetPlayerIdentityTokenState : SessionState
    {
        private readonly State nextState;
        private Future<PlayerIdentityTokenResponse?> pitResponse;

        public GetPlayerIdentityTokenState(State nextState, UIManager manager, ConnectionStateMachine owner) : base(manager, owner)
        {
            this.nextState = nextState;
        }

        public override void StartState()
        {
            pitResponse = DevelopmentAuthentication.CreateDevelopmentPlayerIdentityTokenAsync(
                RuntimeConfigDefaults.LocatorHost,
                RuntimeConfigDefaults.AnonymousAuthenticationPort,
                new PlayerIdentityTokenRequest
                {
                    DevelopmentAuthenticationTokenId = Owner.Blackboard.DevAuthToken,
                    PlayerId = Owner.Blackboard.PlayerName,
                    DisplayName = string.Empty,
                }
            );
        }

        public override void ExitState()
        {
            pitResponse.Dispose();
        }

        public override void Tick()
        {
            if (!pitResponse.TryGet(out var result))
            {
                return;
            }

            if (!result.HasValue || result.Value.Status.Code != ConnectionStatusCode.Success)
            {
                Manager.ScreenManager.StartScreenManager.ShowFailedToGetDeploymentsText(
                    $"Failed to retrieve player identity token.\n Error code: {result.Value.Status.Code}");
                Owner.SetState(Owner.StartState, 2f);

                UnityEngine.Debug.LogWarning($"{result.Value.Status.Code} - {result.Value.Status.Detail}");
            }

            Owner.Blackboard.PlayerIdentityToken = result.Value.PlayerIdentityToken;
            Owner.SetState(new GetDeploymentsState(nextState, Manager, Owner));
        }
    }
}