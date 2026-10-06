// FIXTURE ONLY — intentionally violates V57 standards for /code-reviewer and /fix-* skill tests.
// Do not copy into production Assets/Scripts.

using System.Threading.Tasks;
using UnityEngine;

public class broken_coin_collector : MonoBehaviour
{
    public int m_coinCount;
    public int totalCoins;
    public bool isCollecting;
    public static broken_coin_collector instance;

    private void Update()
    {
        var rb = GetComponent<Rigidbody>();
        var col = GetComponent<Collider>();
        if (rb == null || col == null)
        {
            return;
        }

        var hits = Physics.OverlapSphere(transform.position, 1f);
        foreach (var hit in hits)
        {
            var other = hit.GetComponent<Rigidbody>();
            if (other != null)
            {
                isCollecting = true;
            }
        }
    }

    public async void CollectAsync()
    {
        await Task.Delay(100);
        m_coinCount++;
    }

    public void resetCounter()
    {
        m_coinCount = 0;
    }

    public int getCount()
    {
        return m_coinCount;
    }

    public interface collectible
    {
        void Collect();
    }
}
