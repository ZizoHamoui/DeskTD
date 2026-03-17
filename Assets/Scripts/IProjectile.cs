using UnityEngine;

public interface IProjectile
{
    void Initialize(Enemy targetEnemy, float damageAmount, Tower sourceTower = null);
}
