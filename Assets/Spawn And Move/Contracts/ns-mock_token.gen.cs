// Generated by dojo-bindgen on Mon, 4 Nov 2024 23:36:28 +0000. Do not modify this file manually.
using System;
using System.Threading.Tasks;
using Dojo;
using Dojo.Starknet;
using UnityEngine;
using dojo_bindings;
using System.Collections.Generic;
using System.Linq;
using Enum = Dojo.Starknet.Enum;

// System definitions for `ns-mock_token` contract
public class Mock_token : MonoBehaviour {
    // The address of this contract
    public string contractAddress;

    
    // Call the `upgrade` system with the specified Account and calldata
    // Returns the transaction hash. Use `WaitForTransaction` to wait for the transaction to be confirmed.
    public async Task<FieldElement> upgrade(Account account, FieldElement new_class_hash) {
        List<dojo.FieldElement> calldata = new List<dojo.FieldElement>();
        calldata.Add(new_class_hash.Inner);

        return await account.ExecuteRaw(new dojo.Call[] {
            new dojo.Call{
                to = new FieldElement(contractAddress).Inner,
                selector = "upgrade",
                calldata = calldata.ToArray()
            }
        });
    }
            
}
        