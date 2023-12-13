using System;
using System.Linq;
using bottlenoselabs.C2CS.Runtime;
using dojo_bindings;
using NUnit.Framework;
using Dojo.Torii;
using Dojo.Starknet;
using UnityEditor.VersionControl;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Tests
{
    private readonly string toriiUrl = "http://0.0.0.0:8080";
    private readonly string rpcUrl = "http://0.0.0.0:5050";
    private readonly string playerKey = "0x028cd7ee02d7f6ec9810e75b930e8e607793b302445abbdee0ac88143f18da20";
    private readonly string playerAddress = "0x0517ececd29116499f4a1b64b094da79ba08dfd54a3edaa316134c41f8160973";
    private readonly string worldAddress = "0x05010c31f127114c6198df8a5239e2b7a5151e1156fb43791e37e7385faa8138";
    private readonly string actionsAddress = "0x031571485922572446df9e3198a891e10d3a48e544544317dbcbb667e15848cd";

    private ToriiClient client;
    private JsonRpcClient provider;
    private Account account;

    // our callbacks will mutate these variables
    // we can use this to check that the callback was called
    // when our account spawns
    private bool modelEntityUpdated = false;
    private bool entityUpdated = false;

    [SetUp]
    public void SetupTorii()
    {
        var entities = new dojo.KeysClause[]
        {
            new()
            {
                model = "Moves",
                keys = new string[]{ playerAddress }
            }
        };

        client = new ToriiClient(toriiUrl, rpcUrl, worldAddress, entities);

        if (client == null) throw new Exception("client is null");

        client.StartSubscription();
    }

    [SetUp]
    public void SetupAccount()
    {
        provider = new JsonRpcClient(rpcUrl);

        var signer = new SigningKey("0x1800000000300000180000000000030000000000003006001800006600");

        account = new Account(provider, signer, playerAddress);
    }

    [Test]
    public void TestAccountAddress()
    {
        var address = account.Address();
        var playerAddressBytes = Enumerable.Range(2, playerAddress.Length - 2)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(playerAddress.Substring(x, 2), 16))
            .ToArray();

        Assert.That(address.data.ToArray(), Is.EqualTo(playerAddressBytes));
    }

    [Test]
    public void TestAccountChainId()
    {
        var chainId = account.ChainId();

        // check chainid?
    }

    [Test]
    public void TestAccountSetBlockId()
    {
        var blockId = new dojo.BlockId
        {
            tag = dojo.BlockId_Tag.BlockTag_,
            block_tag = dojo.BlockTag.Pending
        };

        account.SetBlockId(blockId);
    }

    [Test, Order(3)]
    public void TestAccountExecuteRaw()
    {
        dojo.Call call = new dojo.Call()
        {
            to = actionsAddress,
            selector = "spawn"
        };

        account.ExecuteRaw(new[] { call });

        // We wait until our callback is called to mark our 
        // entity as updated. We timeout after 5 seconds.
        var start = DateTime.Now;
        while (!(entityUpdated || modelEntityUpdated) && DateTime.Now - start < TimeSpan.FromSeconds(5))
        {
        }

        
        if (entityUpdated != modelEntityUpdated) {
            Debug.LogWarning("Entity update status mismatch. One of the callbacks was not called.");
            Debug.LogWarning("entityUpdated != modelEntityUpdated");
        }
        Assert.That(entityUpdated || modelEntityUpdated, Is.True);
    }

    [Test]
    public void TestWorldMetadata()
    {
        var worldMetadata = client.WorldMetadata();

        var worldAddressBytes = Enumerable.Range(2, worldAddress.Length - 2)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(worldAddress.Substring(x, 2), 16))
            .ToArray();

        // models should correspond to Moves and Position
        var movesExists = false;
        var positionExists = false;
        foreach (var cHashItemCCharModelMetadata in worldMetadata.models)
        {
            var modelMetadata = cHashItemCCharModelMetadata.value;
            switch (modelMetadata.name)
            {
                case "":

                case "Moves":
                    movesExists = true;

                    Assert.That(modelMetadata.schema.tag, Is.EqualTo(dojo.Ty_Tag.TyStruct));
                    Assert.That(modelMetadata.schema.ty_struct.children[0].name, Is.EqualTo("player"));
                    Assert.That(modelMetadata.schema.ty_struct.children[1].name, Is.EqualTo("remaining"));
                    Assert.That(modelMetadata.schema.ty_struct.children[2].name, Is.EqualTo("last_direction"));

                    // maybe worth verifying the field types?

                    break;
                case "Position":
                    positionExists = true;

                    Assert.That(modelMetadata.schema.tag, Is.EqualTo(dojo.Ty_Tag.TyStruct));
                    Assert.That(modelMetadata.schema.ty_struct.children[0].name, Is.EqualTo("player"));
                    Assert.That(modelMetadata.schema.ty_struct.children[1].name, Is.EqualTo("vec"));

                    // maybe worth verifying the field types?

                    break;
            }
        }

        Assert.That(worldMetadata.world_address.data.ToArray(), Is.EqualTo(worldAddressBytes));
        Assert.That(movesExists, Is.True);
        Assert.That(positionExists, Is.True);
    }

    [Test]
    public void TestEntities()
    {
        var query = new dojo.Query
        {
            limit = 5,
            clause = new dojo.COption_Clause{
                tag = dojo.COption_Clause_Tag.None_Clause,
            }
        };

        var entities = client.Entities(query);
        Assert.That(entities.Count, Is.EqualTo(1));
    }

    [Test]
    public void TestModel()
    {
        var query = new dojo.KeysClause
        {
            model = "Moves",
            keys = new[] { playerKey }
        };

        var model = client.Model(query);

        Assert.That(model.struct_.name, Is.EqualTo("Moves"));
        Assert.That(model.struct_.children[0].name, Is.EqualTo("player"));
    }

    [Test, Order(1)]
    public void TestAddModelsToSync()
    {
        var models = new dojo.KeysClause[]
            { new() { _model = CString.FromString("Moves"), keys = new[] { playerAddress } } };
        client.AddModelsToSync(models);

        var subscribedModels = client.SubscribedModels();

        for (var i = 0; i < subscribedModels.Length; i++)
        {
            Assert.That(subscribedModels[i].model, Is.EqualTo("Moves"));
            Assert.That(subscribedModels[i].keys[0], Is.EqualTo(playerAddress));
        }
    }

    [Test, Order(4)]
    public void TestRemoveModelsToSync()
    {
        var models = new dojo.KeysClause[] { new() { model = "Moves", keys = new[] { playerAddress } } };
        client.RemoveModelsToSync(models);

        var subscribedmodels = client.SubscribedModels();
        Assert.That(subscribedmodels.Length, Is.EqualTo(0));
    }

    [Test, Order(2)]
    public void TestOnEntityStateUpdate()
    {
        dojo.FnPtr_FieldElement_CArrayModel_Void.@delegate callback = (key, models) =>
        {
            entityUpdated = true;
        };
        client.OnEntityStateUpdate(new dojo.FieldElement[] { dojo.felt_from_hex_be(CString.FromString(playerKey)).ok }, callback);
    }

    [Test, Order(2)]
    public void TestOnSyncModelUpdate()
    {
        dojo.FnPtr_Void.@delegate callback = () =>
        {
            modelEntityUpdated = true;
        };
        client.OnSyncModelUpdate(new dojo.KeysClause { model = "Moves", keys = new[] { playerAddress } }, callback);
    }
}