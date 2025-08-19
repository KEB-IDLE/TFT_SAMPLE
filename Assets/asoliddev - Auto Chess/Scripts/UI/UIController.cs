using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public ChampionShop championShop;
    public GamePlayController gamePlayController;

    [Header("Shop Containers (auto-bind if empty)")]
    public GameObject[] championsFrameArray;            // champ container 0..4
    [SerializeField] private Transform championContainersParent; // 비워두면 자동: "Canvas/Shop/layout"
    [SerializeField] private int expectedShopSlots = 5; // 기본 5칸

    public GameObject[] bonusPanels;

    public Text timerText;
    public Text championCountText;
    public Text goldText;
    public Text hpText;

    public GameObject shop;
    public GameObject restartButton;
    public GameObject placementText;
    public GameObject gold;
    public GameObject bonusContainer;
    public GameObject bonusUIPrefab;

    void Awake()
    {
        TryAutoBindShopFrames();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) TryAutoBindShopFrames(false);
    }
#endif

    // ============ Auto-bind ============

    bool TryAutoBindShopFrames(bool logIfFail = true)
    {
        // 이미 제대로 묶여 있으면 패스
        if (championsFrameArray != null &&
            championsFrameArray.Length >= expectedShopSlots &&
            championsFrameArray[0] != null)
            return true;

        // 부모 자동 탐색
        if (championContainersParent == null)
        {
            var go = GameObject.Find("Canvas/Shop/layout");
            if (go != null) championContainersParent = go.transform;
        }

        if (championContainersParent == null)
        {
            if (logIfFail) Debug.LogWarning("UIController: championContainersParent를 찾지 못했습니다. (Canvas/Shop/layout 경로 확인)");
            return false;
        }

        var list = new List<GameObject>();
        for (int i = 0; i < expectedShopSlots; i++)
        {
            // 씬 계층 이름이 정확해야 합니다.
            var t = championContainersParent.Find($"champion container {i}");
            if (t != null) list.Add(t.gameObject);
        }

        if (list.Count == 0)
        {
            if (logIfFail) Debug.LogWarning("UIController: 'champion container N'들을 찾지 못했습니다. 계층 이름 확인하세요.");
            return false;
        }

        championsFrameArray = list.ToArray();
        return true;
    }

    // ============ 기존 UI 이벤트 ============

    public void OnChampionClicked()
    {
        string name = EventSystem.current.currentSelectedGameObject.transform.parent.name;
        string defaultName = "champion container ";
        int championFrameIndex = int.Parse(name.Substring(defaultName.Length, 1));
        championShop.OnChampionFrameClicked(championFrameIndex);
    }

    public void Refresh_Click() { championShop.RefreshShop(false); }
    public void BuyXP_Click()   { championShop.BuyLvl(); }
    public void Restart_Click() { gamePlayController.RestartGame(); }

    public void HideChampionFrame(int index)
    {
        if (!EnsureFrames(index)) return;
        championsFrameArray[index].transform.Find("champion")?.gameObject.SetActive(false);
    }

    public void ShowShopItems()
    {
        if (!TryAutoBindShopFrames()) return;
        for (int i = 0; i < championsFrameArray.Length; i++)
            championsFrameArray[i].transform.Find("champion")?.gameObject.SetActive(true);
    }

    /// <summary>상점 카드 1칸 갱신</summary>
    public void LoadShopItem(Champion champion, int index)
    {
        // ★ 런타임에서도 한 번 더 안전 바인딩
        if (!EnsureFrames(index)) return;

        // get unit frames
        var championUI = championsFrameArray[index].transform.Find("champion");
        if (!championUI)
        {
            Debug.LogError($"UIController: '{championsFrameArray[index].name}' 하위에 'champion' 오브젝트가 없습니다.");
            return;
        }

        var top    = championUI.Find("top");
        var bottom = championUI.Find("bottom");
        if (!top || !bottom)
        {
            Debug.LogError("UIController: 'top' 또는 'bottom' 경로를 찾지 못했습니다.");
            return;
        }

        var type1 = top.Find("type 1");
        var type2 = top.Find("type 2");
        var nameT = bottom.Find("Name");
        var costT = bottom.Find("Cost");
        var icon1 = top.Find("icon 1");
        var icon2 = top.Find("icon 2");

        // ✅ 프리팹에 붙인 PrefabIcon.icon을 top/Image에 바로 세팅
        var topImage = top.Find("Image")?.GetComponent<Image>();
        if (topImage)
        {
            Sprite s = null;
            if (champion && champion.prefab)
            {
                var provider = champion.prefab.GetComponentInChildren<PrefabIcon>(true);
                if (provider) s = provider.icon;
            }
            topImage.sprite = s;
            topImage.enabled = (s != null);
            topImage.preserveAspect = true;
        }

        // 텍스트/타입 아이콘
        if (nameT) nameT.GetComponent<Text>().text = champion ? champion.uiname : "";
        if (costT) costT.GetComponent<Text>().text = champion ? champion.cost.ToString() : "";
        if (type1) type1.GetComponent<Text>().text = champion ? champion.type1.displayName : "";
        if (type2) type2.GetComponent<Text>().text = champion ? champion.type2.displayName : "";
        if (icon1) icon1.GetComponent<Image>().sprite = champion ? champion.type1.icon : null;
        if (icon2) icon2.GetComponent<Image>().sprite = champion ? champion.type2.icon : null;
    }

    bool EnsureFrames(int index)
    {
        if (!TryAutoBindShopFrames())
        {
            Debug.LogError("UIController: championsFrameArray가 비어있고 자동 바인딩도 실패했습니다. 인스펙터에 5개 컨테이너를 연결하세요.");
            return false;
        }
        if (index < 0 || index >= championsFrameArray.Length || championsFrameArray[index] == null)
        {
            Debug.LogError($"UIController: championsFrameArray[{index}]가 비어있습니다. 계층 이름/개수를 확인하세요.");
            return false;
        }
        return true;
    }

    // ===== 기존 UI 갱신 =====

    public void UpdateUI()
    {
        goldText.text = gamePlayController.currentGold.ToString();
        championCountText.text = $"{gamePlayController.currentChampionCount} / {gamePlayController.currentChampionLimit}";
        hpText.text = $"HP {gamePlayController.currentHP}";

        foreach (var go in bonusPanels) go.SetActive(false);

        if (gamePlayController.championTypeCount != null)
        {
            int i = 0;
            foreach (var m in gamePlayController.championTypeCount)
            {
                if (i >= bonusPanels.Length) break;
                var bonusUI = bonusPanels[i];
                bonusUI.transform.SetParent(bonusContainer.transform);
                bonusUI.transform.Find("icon").GetComponent<Image>().sprite = m.Key.icon;
                bonusUI.transform.Find("name").GetComponent<Text>().text = m.Key.displayName;
                bonusUI.transform.Find("count").GetComponent<Text>().text = $"{m.Value} / {m.Key.championBonus.championCount}";
                bonusUI.SetActive(true);
                i++;
            }
        }
    }

    public void UpdateTimerText()           { timerText.text = gamePlayController.timerDisplay.ToString(); }
    public void SetTimerTextActive(bool b)  { timerText.gameObject.SetActive(b); placementText.SetActive(b); }
    public void ShowLossScreen()            { SetTimerTextActive(false); shop.SetActive(false); gold.SetActive(false); restartButton.SetActive(true); }
    public void ShowGameScreen()            { SetTimerTextActive(true);  shop.SetActive(true);  gold.SetActive(true);  restartButton.SetActive(false); }
}
