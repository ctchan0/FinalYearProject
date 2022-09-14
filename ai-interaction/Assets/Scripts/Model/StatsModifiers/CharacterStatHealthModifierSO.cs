using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CharacterStatHealthModifierSO : CharacterStatModifierSO
{
    public override void AffectCharacter(GameObject adventurer, float val)
    {
        adventurer.GetComponent<AdventurerAgent>().GetCure((int)val);
    }
}
