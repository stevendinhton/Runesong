using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Creature {
    
    public struct CreatureActive : IComponentData
    {

    }

    public struct Creature
    {
        Name name;
        Health health;
        Hunger hunger;

        int id;
        bool alive;
    }

    public struct Health
    {
        int maxHealth;
        int currentHealth;
    }

    public struct Hunger
    {
        int maxHunger;
        int currentHunger;
    }

    public struct Name
    {
        string firstName;
        string lastName;
        string nickName;
    }
}
