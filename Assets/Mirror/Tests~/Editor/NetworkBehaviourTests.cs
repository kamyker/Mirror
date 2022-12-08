using Mirror.RemoteCalls;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.Tests
{
    class EmptyBehaviour : NetworkBehaviour {}

    class SyncVarGameObjectEqualExposedBehaviour : NetworkBehaviour
    {
        public bool SyncVarGameObjectEqualExposed(GameObject newGameObject, uint netIdField)
        {
            return SyncVarGameObjectEqual(newGameObject, netIdField);
        }
    }

    class SyncVarNetworkIdentityEqualExposedBehaviour : NetworkBehaviour
    {
        public bool SyncVarNetworkIdentityEqualExposed(NetworkIdentity newNetworkIdentity, uint netIdField)
        {
            return SyncVarNetworkIdentityEqual(newNetworkIdentity, netIdField);
        }
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class NetworkBehaviourDelegateComponent : NetworkBehaviour
    {
        public static void Delegate(NetworkBehaviour comp, NetworkReader reader, NetworkConnection senderConnection) {}
        public static void Delegate2(NetworkBehaviour comp, NetworkReader reader, NetworkConnection senderConnection) {}
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class NetworkBehaviourSetSyncVarGameObjectComponent : NetworkBehaviour
    {
        //[SyncVar]
        public GameObject test;
        // usually generated by weaver
        public uint testNetId;

        // SetSyncVarGameObject wrapper to expose it
        public void SetSyncVarGameObjectExposed(GameObject newGameObject, ulong dirtyBit) =>
            SetSyncVarGameObject(newGameObject, ref test, dirtyBit, ref testNetId);
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class NetworkBehaviourGetSyncVarGameObjectComponent : NetworkBehaviour
    {
        //[SyncVar]
        public GameObject test;
        // usually generated by weaver
        public uint testNetId;

        // SetSyncVarGameObject wrapper to expose it
        public GameObject GetSyncVarGameObjectExposed() =>
            GetSyncVarGameObject(testNetId, ref test);
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class NetworkBehaviourSetSyncVarNetworkIdentityComponent : NetworkBehaviour
    {
        //[SyncVar]
        public NetworkIdentity test;
        // usually generated by weaver
        public uint testNetId;

        // SetSyncVarNetworkIdentity wrapper to expose it
        public void SetSyncVarNetworkIdentityExposed(NetworkIdentity newNetworkIdentity, ulong dirtyBit) =>
            SetSyncVarNetworkIdentity(newNetworkIdentity, ref test, dirtyBit, ref testNetId);
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class NetworkBehaviourGetSyncVarNetworkIdentityComponent : NetworkBehaviour
    {
        //[SyncVar]
        public NetworkIdentity test;
        // usually generated by weaver
        public uint testNetId;

        // SetSyncVarNetworkIdentity wrapper to expose it
        public NetworkIdentity GetSyncVarNetworkIdentityExposed() =>
            GetSyncVarNetworkIdentity(testNetId, ref test);
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class OnStopClientComponent : NetworkBehaviour
    {
        public int called;
        public override void OnStopClient() => ++called;
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class OnStartClientComponent : NetworkBehaviour
    {
        public int called;
        public override void OnStartClient() => ++called;
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class OnStartLocalPlayerComponent : NetworkBehaviour
    {
        public int called;
        public override void OnStartLocalPlayer() => ++called;
    }

    public class NetworkBehaviourTests : MirrorEditModeTest
    {
        [TearDown]
        public override void TearDown()
        {
            NetworkServer.RemoveLocalConnection();
            base.TearDown();
        }

        [Test]
        public void IsServerOnly()
        {
            CreateNetworked(out _, out NetworkIdentity identity, out EmptyBehaviour emptyBehaviour);

            // call OnStartServer so isServer is true
            identity.OnStartServer();
            Assert.That(identity.isServer, Is.True);

            // isServerOnly should be true when isServer = true && isClient = false
            Assert.That(emptyBehaviour.isServer, Is.True);
            Assert.That(emptyBehaviour.isClient, Is.False);
            Assert.That(emptyBehaviour.isServerOnly, Is.True);
        }

        [Test]
        public void IsClientOnly()
        {
            CreateNetworked(out _, out NetworkIdentity identity, out EmptyBehaviour emptyBehaviour);

            // isClientOnly should be true when isServer = false && isClient = true
            identity.isClient = true;
            Assert.That(emptyBehaviour.isServer, Is.False);
            Assert.That(emptyBehaviour.isClient, Is.True);
            Assert.That(emptyBehaviour.isClientOnly, Is.True);
        }

        [Test]
        public void HasNoAuthorityByDefault()
        {
            // no authority by default
            CreateNetworked(out _, out _, out EmptyBehaviour emptyBehaviour);
            Assert.That(emptyBehaviour.hasAuthority, Is.False);
        }

        [Test]
        public void HasIdentitysNetId()
        {
            CreateNetworked(out _, out NetworkIdentity identity, out EmptyBehaviour emptyBehaviour);
            identity.netId = 42;
            Assert.That(emptyBehaviour.netId, Is.EqualTo(42));
        }

        [Test]
        public void HasIdentitysConnectionToServer()
        {
            CreateNetworked(out _, out NetworkIdentity identity, out EmptyBehaviour emptyBehaviour);
            identity.connectionToServer = new LocalConnectionToServer();
            Assert.That(emptyBehaviour.connectionToServer, Is.EqualTo(identity.connectionToServer));
        }

        [Test]
        public void HasIdentitysConnectionToClient()
        {
            CreateNetworked(out _, out NetworkIdentity identity, out EmptyBehaviour emptyBehaviour);
            identity.connectionToClient = new LocalConnectionToClient();
            Assert.That(emptyBehaviour.connectionToClient, Is.EqualTo(identity.connectionToClient));
        }

        [Test]
        public void ComponentIndex()
        {
            // create a NetworkIdentity with two components
            CreateNetworked(out GameObject _, out NetworkIdentity _, out EmptyBehaviour first, out EmptyBehaviour second);
            Assert.That(first.ComponentIndex, Is.EqualTo(0));
            Assert.That(second.ComponentIndex, Is.EqualTo(1));
        }

        [Test, Ignore("NetworkServerTest.SendCommand does it already")]
        public void SendCommandInternal() {}

        [Test, Ignore("ClientRpcTest.cs tests Rpcs already")]
        public void SendRPCInternal() {}

        [Test, Ignore("TargetRpcTest.cs tests TargetRpcs already")]
        public void SendTargetRPCInternal() {}

        [Test]
        public void RegisterDelegateDoesntOverwrite()
        {
            // registerdelegate is protected, but we can use
            // RegisterCommandDelegate which calls RegisterDelegate
            int registeredHash1 = RemoteCallHelper.RegisterDelegate(
                typeof(NetworkBehaviourDelegateComponent),
                nameof(NetworkBehaviourDelegateComponent.Delegate),
                MirrorInvokeType.Command,
                NetworkBehaviourDelegateComponent.Delegate,
                false);

            // registering the exact same one should be fine. it should simply
            // do nothing.
            int registeredHash2 = RemoteCallHelper.RegisterDelegate(
                typeof(NetworkBehaviourDelegateComponent),
                nameof(NetworkBehaviourDelegateComponent.Delegate),
                MirrorInvokeType.Command,
                NetworkBehaviourDelegateComponent.Delegate,
                false);

            // registering the same name with a different callback shouldn't
            // work
            LogAssert.Expect(LogType.Error, $"Function {typeof(NetworkBehaviourDelegateComponent)}.{nameof(NetworkBehaviourDelegateComponent.Delegate)} and {typeof(NetworkBehaviourDelegateComponent)}.{nameof(NetworkBehaviourDelegateComponent.Delegate2)} have the same hash.  Please rename one of them");
            int registeredHash3 = RemoteCallHelper.RegisterDelegate(
                typeof(NetworkBehaviourDelegateComponent),
                nameof(NetworkBehaviourDelegateComponent.Delegate),
                MirrorInvokeType.Command,
                NetworkBehaviourDelegateComponent.Delegate2,
                false);

            // clean up
            RemoteCallHelper.RemoveDelegate(registeredHash1);
            RemoteCallHelper.RemoveDelegate(registeredHash2);
            RemoteCallHelper.RemoveDelegate(registeredHash3);
        }

        [Test]
        public void GetDelegate()
        {
            // registerdelegate is protected, but we can use
            // RegisterCommandDelegate which calls RegisterDelegate
            int registeredHash = RemoteCallHelper.RegisterDelegate(
                typeof(NetworkBehaviourDelegateComponent),
                nameof(NetworkBehaviourDelegateComponent.Delegate),
                MirrorInvokeType.Command,
                NetworkBehaviourDelegateComponent.Delegate,
                false);

            // get handler
            int cmdHash = RemoteCallHelper.GetMethodHash(typeof(NetworkBehaviourDelegateComponent), nameof(NetworkBehaviourDelegateComponent.Delegate));
            CmdDelegate func = RemoteCallHelper.GetDelegate(cmdHash);
            CmdDelegate expected = NetworkBehaviourDelegateComponent.Delegate;
            Assert.That(func, Is.EqualTo(expected));

            // invalid hash should return null handler
            CmdDelegate funcNull = RemoteCallHelper.GetDelegate(1234);
            Assert.That(funcNull, Is.Null);

            // clean up
            RemoteCallHelper.RemoveDelegate(registeredHash);
        }

        // NOTE: SyncVarGameObjectEqual should be static later
        [Test]
        public void SyncVarGameObjectEqualZeroNetIdNullIsTrue()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out SyncVarGameObjectEqualExposedBehaviour comp);
            // null and identity.netid==0 returns true (=equal)
            //
            // later we should reevaluate if this is so smart or not. might be
            // better to return false here.
            // => we possibly return false so that resync doesn't happen when
            //    GO disappears? or not?
            bool result = comp.SyncVarGameObjectEqualExposed(null, identity.netId);
            Assert.That(result, Is.True);
        }

        // NOTE: SyncVarGameObjectEqual should be static later
        [Test]
        public void SyncVarGameObjectEqualNull()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out SyncVarGameObjectEqualExposedBehaviour comp);
            // our identity should have a netid for comparing
            identity.netId = 42;

            // null should return false
            bool result = comp.SyncVarGameObjectEqualExposed(null, identity.netId);
            Assert.That(result, Is.False);
        }

        // NOTE: SyncVarGameObjectEqual should be static later
        [Test]
        public void SyncVarGameObjectEqualZeroNetIdAndGOWithoutIdentityComponentIsTrue()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);
            CreateNetworked(out GameObject go, out NetworkIdentity _, out SyncVarGameObjectEqualExposedBehaviour comp);

            // null and identity.netid==0 returns true (=equal)
            //
            // later we should reevaluate if this is so smart or not. might be
            // better to return false here.
            // => we possibly return false so that resync doesn't happen when
            //    GO disappears? or not?
            bool result = comp.SyncVarGameObjectEqualExposed(go, identity.netId);
            Assert.That(result, Is.True);
        }

        // NOTE: SyncVarGameObjectEqual should be static later
        [Test]
        public void SyncVarGameObjectEqualWithoutIdentityComponent()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // our identity should have a netid for comparing
            identity.netId = 42;

            // gameobject without networkidentity component should return false
            CreateNetworked(out GameObject go, out NetworkIdentity _, out SyncVarGameObjectEqualExposedBehaviour comp);
            bool result = comp.SyncVarGameObjectEqualExposed(go, identity.netId);
            Assert.That(result, Is.False);
        }

        // NOTE: SyncVarGameObjectEqual should be static later
        [Test]
        public void SyncVarGameObjectEqualValidGOWithDifferentNetId()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // our identity should have a netid for comparing
            identity.netId = 42;

            // gameobject with valid networkidentity and netid that is different
            CreateNetworked(out GameObject go, out NetworkIdentity ni, out SyncVarGameObjectEqualExposedBehaviour comp);
            ni.netId = 43;
            bool result = comp.SyncVarGameObjectEqualExposed(go, identity.netId);
            Assert.That(result, Is.False);
        }

        // NOTE: SyncVarGameObjectEqual should be static later
        [Test]
        public void SyncVarGameObjectEqualValidGOWithSameNetId()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // our identity should have a netid for comparing
            identity.netId = 42;

            // gameobject with valid networkidentity and netid that is different
            CreateNetworked(out GameObject go, out NetworkIdentity ni, out SyncVarGameObjectEqualExposedBehaviour comp);
            ni.netId = 42;
            bool result = comp.SyncVarGameObjectEqualExposed(go, identity.netId);
            Assert.That(result, Is.True);
        }

        // NOTE: SyncVarGameObjectEqual should be static later
        [Test]
        public void SyncVarGameObjectEqualUnspawnedGO()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // our identity should have a netid for comparing
            identity.netId = 42;

            // gameobject with valid networkidentity and 0 netid that is unspawned
            CreateNetworked(out GameObject go, out NetworkIdentity ni, out SyncVarGameObjectEqualExposedBehaviour comp);
            LogAssert.Expect(LogType.Warning, $"SetSyncVarGameObject GameObject {go} has a zero netId. Maybe it is not spawned yet?");
            bool result = comp.SyncVarGameObjectEqualExposed(go, identity.netId);
            Assert.That(result, Is.False);
        }

        // NOTE: SyncVarGameObjectEqual should be static later
        [Test]
        public void SyncVarGameObjectEqualUnspawnedGOZeroNetIdIsTrue()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // unspawned go and identity.netid==0 returns true (=equal)
            CreateNetworked(out GameObject go, out NetworkIdentity ni, out SyncVarGameObjectEqualExposedBehaviour comp);
            LogAssert.Expect(LogType.Warning, $"SetSyncVarGameObject GameObject {go} has a zero netId. Maybe it is not spawned yet?");
            bool result = comp.SyncVarGameObjectEqualExposed(go, identity.netId);
            Assert.That(result, Is.True);
        }

        // NOTE: SyncVarNetworkIdentityEqual should be static later
        [Test]
        public void SyncVarNetworkIdentityEqualZeroNetIdNullIsTrue()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out SyncVarNetworkIdentityEqualExposedBehaviour comp);

            // null and identity.netid==0 returns true (=equal)
            //
            // later we should reevaluate if this is so smart or not. might be
            // better to return false here.
            // => we possibly return false so that resync doesn't happen when
            //    GO disappears? or not?
            bool result = comp.SyncVarNetworkIdentityEqualExposed(null, identity.netId);
            Assert.That(result, Is.True);
        }

        // NOTE: SyncVarNetworkIdentityEqual should be static later
        [Test]
        public void SyncVarNetworkIdentityEqualNull()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out SyncVarNetworkIdentityEqualExposedBehaviour comp);

            // our identity should have a netid for comparing
            identity.netId = 42;

            // null should return false
            bool result = comp.SyncVarNetworkIdentityEqualExposed(null, identity.netId);
            Assert.That(result, Is.False);
        }

        // NOTE: SyncVarNetworkIdentityEqual should be static later
        [Test]
        public void SyncVarNetworkIdentityEqualValidIdentityWithDifferentNetId()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // our identity should have a netid for comparing
            identity.netId = 42;

            // gameobject with valid networkidentity and netid that is different
            CreateNetworked(out GameObject go, out NetworkIdentity ni, out SyncVarNetworkIdentityEqualExposedBehaviour comp);
            ni.netId = 43;
            bool result = comp.SyncVarNetworkIdentityEqualExposed(ni, identity.netId);
            Assert.That(result, Is.False);
        }

        // NOTE: SyncVarNetworkIdentityEqual should be static later
        [Test]
        public void SyncVarNetworkIdentityEqualValidIdentityWithSameNetId()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // our identity should have a netid for comparing
            identity.netId = 42;

            // gameobject with valid networkidentity and netid that is different
            CreateNetworked(out GameObject _, out NetworkIdentity ni, out SyncVarNetworkIdentityEqualExposedBehaviour comp);
            ni.netId = 42;
            bool result = comp.SyncVarNetworkIdentityEqualExposed(ni, identity.netId);
            Assert.That(result, Is.True);
        }

        // NOTE: SyncVarNetworkIdentityEqual should be static later
        [Test]
        public void SyncVarNetworkIdentityEqualUnspawnedIdentity()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // our identity should have a netid for comparing
            identity.netId = 42;

            // gameobject with valid networkidentity and 0 netid that is unspawned
            CreateNetworked(out GameObject go, out NetworkIdentity ni, out SyncVarNetworkIdentityEqualExposedBehaviour comp);
            LogAssert.Expect(LogType.Warning, $"SetSyncVarNetworkIdentity NetworkIdentity {ni} has a zero netId. Maybe it is not spawned yet?");
            bool result = comp.SyncVarNetworkIdentityEqualExposed(ni, identity.netId);
            Assert.That(result, Is.False);
        }

        // NOTE: SyncVarNetworkIdentityEqual should be static later
        [Test]
        public void SyncVarNetworkIdentityEqualUnspawnedIdentityZeroNetIdIsTrue()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // unspawned go and identity.netid==0 returns true (=equal)
            CreateNetworked(out GameObject go, out NetworkIdentity ni, out SyncVarNetworkIdentityEqualExposedBehaviour comp);
            LogAssert.Expect(LogType.Warning, $"SetSyncVarNetworkIdentity NetworkIdentity {ni} has a zero netId. Maybe it is not spawned yet?");
            bool result = comp.SyncVarNetworkIdentityEqualExposed(ni, identity.netId);
            Assert.That(result, Is.True);
        }

        [Test]
        public void SetSyncVarGameObjectWithValidObject()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity _, out NetworkBehaviourSetSyncVarGameObjectComponent comp);

            // create a valid GameObject with networkidentity and netid
            CreateNetworked(out GameObject go, out NetworkIdentity ni);
            ni.netId = 43;

            // set the GameObject SyncVar
            Assert.That(comp.IsDirty(), Is.False);
            comp.SetSyncVarGameObjectExposed(go, 1ul);
            Assert.That(comp.test, Is.EqualTo(go));
            Assert.That(comp.testNetId, Is.EqualTo(ni.netId));
            Assert.That(comp.IsDirty(), Is.True);
        }

        [Test]
        public void SetSyncVarGameObjectNull()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity _, out NetworkBehaviourSetSyncVarGameObjectComponent comp);

            // set some existing GO+netId first to check if it is going to be
            // overwritten
            CreateGameObject(out GameObject go);
            comp.test = go;
            comp.testNetId = 43;

            // set the GameObject SyncVar to null
            Assert.That(comp.IsDirty(), Is.False);
            comp.SetSyncVarGameObjectExposed(null, 1ul);
            Assert.That(comp.test, Is.EqualTo(null));
            Assert.That(comp.testNetId, Is.EqualTo(0));
            Assert.That(comp.IsDirty(), Is.True);
        }

        [Test]
        public void SetSyncVarGameObjectWithoutNetworkIdentity()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity _, out NetworkBehaviourSetSyncVarGameObjectComponent comp);

            // set some existing GO+netId first to check if it is going to be
            // overwritten
            CreateGameObject(out GameObject go);
            comp.test = go;
            comp.testNetId = 43;

            // create test GO with no networkidentity
            CreateGameObject(out GameObject test);

            // set the GameObject SyncVar to 'test' GO without netidentity.
            // -> the way it currently works is that it sets netId to 0, but
            //    it DOES set gameObjectField to the GameObject without the
            //    NetworkIdentity, instead of setting it to null because it's
            //    seemingly invalid.
            // -> there might be a deeper reason why UNET did that. perhaps for
            //    cases where we assign a GameObject before the network was
            //    fully started and has no netId or networkidentity yet etc.
            // => it works, so let's keep it for now
            Assert.That(comp.IsDirty(), Is.False);
            comp.SetSyncVarGameObjectExposed(test, 1ul);
            Assert.That(comp.test, Is.EqualTo(test));
            Assert.That(comp.testNetId, Is.EqualTo(0));
            Assert.That(comp.IsDirty(), Is.True);
        }

        [Test]
        public void SetSyncVarGameObjectZeroNetId()
        {
            CreateNetworked(out GameObject gameObject, out NetworkIdentity identity, out NetworkBehaviourSetSyncVarGameObjectComponent comp);

            // set some existing GO+netId first to check if it is going to be
            // overwritten
            CreateGameObject(out GameObject go);
            comp.test = go;
            comp.testNetId = 43;

            // create test GO with networkidentity and zero netid
            CreateNetworked(out GameObject test, out NetworkIdentity ni);
            Assert.That(ni.netId, Is.EqualTo(0));

            // set the GameObject SyncVar to 'test' GO with zero netId.
            // -> the way it currently works is that it sets netId to 0, but
            //    it DOES set gameObjectField to the GameObject without the
            //    NetworkIdentity, instead of setting it to null because it's
            //    seemingly invalid.
            // -> there might be a deeper reason why UNET did that. perhaps for
            //    cases where we assign a GameObject before the network was
            //    fully started and has no netId or networkidentity yet etc.
            // => it works, so let's keep it for now
            Assert.That(comp.IsDirty(), Is.False);
            LogAssert.Expect(LogType.Warning, $"SetSyncVarGameObject GameObject {test} has a zero netId. Maybe it is not spawned yet?");
            comp.SetSyncVarGameObjectExposed(test, 1ul);
            Assert.That(comp.test, Is.EqualTo(test));
            Assert.That(comp.testNetId, Is.EqualTo(0));
            Assert.That(comp.IsDirty(), Is.True);
        }

        [Test]
        public void GetSyncVarGameObjectOnServer()
        {
            CreateNetworked(out GameObject gameObject, out NetworkIdentity identity, out NetworkBehaviourGetSyncVarGameObjectComponent comp);

            // call OnStartServer so isServer is true
            identity.OnStartServer();
            Assert.That(identity.isServer, Is.True);

            // create a syncable GameObject
            CreateNetworked(out GameObject go, out NetworkIdentity ni);
            ni.netId = identity.netId + 1;

            // assign it in the component
            comp.test = go;
            comp.testNetId = ni.netId;

            // get it on the server. should simply return the field instead of
            // doing any netId lookup like on the client
            GameObject result = comp.GetSyncVarGameObjectExposed();
            Assert.That(result, Is.EqualTo(go));

            // clean up: set isServer false first, otherwise Destroy instead of DestroyImmediate is called
            identity.netId = 0;
        }

        [Test]
        public void GetSyncVarGameObjectOnServerNull()
        {
            CreateNetworked(out GameObject gameObject, out NetworkIdentity identity, out NetworkBehaviourGetSyncVarGameObjectComponent comp);

            // call OnStartServer and assign netId so isServer is true
            identity.OnStartServer();
            Assert.That(identity.isServer, Is.True);

            // get it on the server. null should work fine.
            GameObject result = comp.GetSyncVarGameObjectExposed();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetSyncVarGameObjectOnClient()
        {
            // start server & connect client because we need spawn functions
            NetworkServer.Listen(1);
            ConnectClientBlockingAuthenticatedAndReady(out _);

            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // are we on client and not on server?
            identity.isClient = true;
            Assert.That(identity.isServer, Is.False);

            // create a networked object with test component
            CreateNetworked(out GameObject _, out NetworkIdentity _, out NetworkBehaviourGetSyncVarGameObjectComponent comp);

            // create a spawned, syncable GameObject
            // (client tries to look up via netid, so needs to be spawned)
            CreateNetworkedAndSpawn(
                out GameObject serverGO, out NetworkIdentity serverIdentity,
                out GameObject clientGO, out NetworkIdentity clientIdentity);

            // assign ONLY netId in the component. assume that GameObject was
            // assigned earlier but client walked so far out of range that it
            // was despawned on the client. so it's forced to do the netId look-
            // up.
            Assert.That(comp.test, Is.Null);
            comp.testNetId = clientIdentity.netId;

            // get it on the client. should look up netId in spawned
            GameObject result = comp.GetSyncVarGameObjectExposed();
            Assert.That(result, Is.EqualTo(clientGO));
        }

        [Test]
        public void GetSyncVarGameObjectOnClientNull()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity);

            // are we on client and not on server?
            identity.isClient = true;
            Assert.That(identity.isServer, Is.False);

            // create a networked object with test component
            CreateNetworked(out GameObject _, out NetworkIdentity _, out NetworkBehaviourGetSyncVarGameObjectComponent comp);

            // get it on the client. null should be supported.
            GameObject result = comp.GetSyncVarGameObjectExposed();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void SetSyncVarNetworkIdentityWithValidObject()
        {
            // create a networked object with test component
            CreateNetworked(out GameObject _, out NetworkIdentity _, out NetworkBehaviourSetSyncVarNetworkIdentityComponent comp);

            // create a valid GameObject with networkidentity and netid
            CreateNetworked(out GameObject _, out NetworkIdentity ni);
            ni.netId = 43;

            // set the NetworkIdentity SyncVar
            Assert.That(comp.IsDirty(), Is.False);
            comp.SetSyncVarNetworkIdentityExposed(ni, 1ul);
            Assert.That(comp.test, Is.EqualTo(ni));
            Assert.That(comp.testNetId, Is.EqualTo(ni.netId));
            Assert.That(comp.IsDirty(), Is.True);
        }

        [Test]
        public void SetSyncVarNetworkIdentityNull()
        {
            // create a networked object with test component
            CreateNetworked(out GameObject _, out NetworkIdentity _, out NetworkBehaviourSetSyncVarNetworkIdentityComponent comp);

            // set some existing NI+netId first to check if it is going to be
            // overwritten
            CreateNetworked(out GameObject _, out NetworkIdentity ni);
            comp.test = ni;
            comp.testNetId = 43;

            // set the NetworkIdentity SyncVar to null
            Assert.That(comp.IsDirty(), Is.False);
            comp.SetSyncVarNetworkIdentityExposed(null, 1ul);
            Assert.That(comp.test, Is.EqualTo(null));
            Assert.That(comp.testNetId, Is.EqualTo(0));
            Assert.That(comp.IsDirty(), Is.True);
        }

        [Test]
        public void SetSyncVarNetworkIdentityZeroNetId()
        {
            CreateNetworked(out _, out _, out NetworkBehaviourSetSyncVarNetworkIdentityComponent comp);

            // set some existing NI+netId first to check if it is going to be
            // overwritten
            CreateNetworked(out GameObject _, out NetworkIdentity ni);
            comp.test = ni;
            comp.testNetId = 43;

            // create test GO with networkidentity and zero netid
            CreateNetworked(out GameObject _, out NetworkIdentity testNi);
            Assert.That(testNi.netId, Is.EqualTo(0));

            // set the NetworkIdentity SyncVar to 'test' GO with zero netId.
            // -> the way it currently works is that it sets netId to 0, but
            //    it DOES set gameObjectField to the GameObject without the
            //    NetworkIdentity, instead of setting it to null because it's
            //    seemingly invalid.
            // -> there might be a deeper reason why UNET did that. perhaps for
            //    cases where we assign a GameObject before the network was
            //    fully started and has no netId or networkidentity yet etc.
            // => it works, so let's keep it for now
            Assert.That(comp.IsDirty(), Is.False);
            LogAssert.Expect(LogType.Warning, $"SetSyncVarNetworkIdentity NetworkIdentity {testNi} has a zero netId. Maybe it is not spawned yet?");
            comp.SetSyncVarNetworkIdentityExposed(testNi, 1ul);
            Assert.That(comp.test, Is.EqualTo(testNi));
            Assert.That(comp.testNetId, Is.EqualTo(0));
            Assert.That(comp.IsDirty(), Is.True);
        }

        [Test]
        public void GetSyncVarNetworkIdentityOnServer()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out NetworkBehaviourGetSyncVarNetworkIdentityComponent comp);

            // call OnStartServer so isServer is true
            identity.OnStartServer();
            Assert.That(identity.isServer, Is.True);

            // create a syncable GameObject
            CreateNetworked(out _, out NetworkIdentity ni);
            ni.netId = identity.netId + 1;

            // assign it in the component
            comp.test = ni;
            comp.testNetId = ni.netId;

            // get it on the server. should simply return the field instead of
            // doing any netId lookup like on the client
            NetworkIdentity result = comp.GetSyncVarNetworkIdentityExposed();
            Assert.That(result, Is.EqualTo(ni));
        }

        [Test]
        public void GetSyncVarNetworkIdentityOnServerNull()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out NetworkBehaviourGetSyncVarNetworkIdentityComponent comp);

            // call OnStartServer so isServer is true
            identity.OnStartServer();
            Assert.That(identity.isServer, Is.True);

            // get it on the server. null should work fine.
            NetworkIdentity result = comp.GetSyncVarNetworkIdentityExposed();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetSyncVarNetworkIdentityOnClient()
        {
            // start server & connect client because we need spawn functions
            NetworkServer.Listen(1);
            ConnectClientBlockingAuthenticatedAndReady(out _);

            CreateNetworked(out GameObject _, out NetworkIdentity identity, out NetworkBehaviourGetSyncVarNetworkIdentityComponent comp);

            // are we on client and not on server?
            identity.isClient = true;
            Assert.That(identity.isServer, Is.False);

            // create a spawned, syncable GameObject
            // (client tries to look up via netid, so needs to be spawned)
            CreateNetworkedAndSpawn(
                out _, out NetworkIdentity serverIdentity,
                out _, out NetworkIdentity clientIdentity);

            // assign ONLY netId in the component. assume that GameObject was
            // assigned earlier but client walked so far out of range that it
            // was despawned on the client. so it's forced to do the netId look-
            // up.
            Assert.That(comp.test, Is.Null);
            comp.testNetId = clientIdentity.netId;

            // get it on the client. should look up netId in spawned
            NetworkIdentity result = comp.GetSyncVarNetworkIdentityExposed();
            Assert.That(result, Is.EqualTo(clientIdentity));
        }

        [Test]
        public void GetSyncVarNetworkIdentityOnClientNull()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out NetworkBehaviourGetSyncVarNetworkIdentityComponent comp);

            // are we on client and not on server?
            identity.isClient = true;
            Assert.That(identity.isServer, Is.False);

            // get it on the client. null should be supported.
            NetworkIdentity result = comp.GetSyncVarNetworkIdentityExposed();
            Assert.That(result, Is.Null);
        }

        [Test]
        public void SerializeAndDeserializeObjectsAll()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity _, out NetworkBehaviourWithSyncVarsAndCollections comp);

            // add values to synclist
            comp.list.Add(42);
            comp.list.Add(43);

            // serialize it
            NetworkWriter writer = new NetworkWriter();
            comp.SerializeObjectsAll(writer);

            // clear original list
            comp.list.Clear();
            Assert.That(comp.list.Count, Is.EqualTo(0));

            // deserialize it
            NetworkReader reader = new NetworkReader(writer.ToArray());
            comp.DeSerializeObjectsAll(reader);
            Assert.That(comp.list.Count, Is.EqualTo(2));
            Assert.That(comp.list[0], Is.EqualTo(42));
            Assert.That(comp.list[1], Is.EqualTo(43));
        }

        [Test]
        public void SerializeAndDeserializeObjectsDelta()
        {
            // SyncLists are only set dirty while owner has observers.
            // need a connection.
            NetworkServer.Listen(1);
            ConnectHostClientBlockingAuthenticatedAndReady();

            CreateNetworkedAndSpawn(out GameObject _, out NetworkIdentity _, out NetworkBehaviourWithSyncVarsAndCollections comp);

            // add to synclist
            comp.list.Add(42);
            comp.list.Add(43);

            // serialize it
            NetworkWriter writer = new NetworkWriter();
            comp.SerializeObjectsDelta(writer);

            // clear original list
            comp.list.Clear();
            Assert.That(comp.list.Count, Is.EqualTo(0));

            // deserialize it
            NetworkReader reader = new NetworkReader(writer.ToArray());
            comp.DeSerializeObjectsDelta(reader);
            Assert.That(comp.list.Count, Is.EqualTo(2));
            Assert.That(comp.list[0], Is.EqualTo(42));
            Assert.That(comp.list[1], Is.EqualTo(43));
        }

        [Test]
        public void OnStopClient()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out OnStopClientComponent comp);
            identity.OnStopClient();
            Assert.That(comp.called, Is.EqualTo(1));
        }

        [Test]
        public void OnStartClient()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out OnStartClientComponent comp);
            identity.OnStartClient();
            Assert.That(comp.called, Is.EqualTo(1));
        }

        [Test]
        public void OnStartLocalPlayer()
        {
            CreateNetworked(out GameObject _, out NetworkIdentity identity, out OnStartLocalPlayerComponent comp);
            identity.OnStartLocalPlayer();
            Assert.That(comp.called, Is.EqualTo(1));
        }
    }

    // we need to inherit from networkbehaviour to test protected functions
    public class NetworkBehaviourInitSyncObjectTester : NetworkBehaviour
    {
        [Test]
        public void InitSyncObject()
        {
            SyncObject syncObject = new SyncList<bool>();
            InitSyncObject(syncObject);
            Assert.That(syncObjects.Count, Is.EqualTo(1));
            Assert.That(syncObjects[0], Is.EqualTo(syncObject));
        }
    }
}
