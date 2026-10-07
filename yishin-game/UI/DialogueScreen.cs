using System;
using System.Collections.Generic;
using System.Drawing;

namespace yishin_game.UI
{
    public class DialogueScreen : UIScreen
    {
        // 對話框位置與尺寸常數（虛擬解析度座標）
        private const float DialogX = 20f; // 縮小左右邊距以增加寬度
        private const float DialogY = 300f; // 提升對話框位置，避免與選項重疊
        private const float DialogW = 1240f; // 加寬對話框
        private const float DialogH = 200f;

        private class Choice
        {
            public string Text = "";
            public string NextId = null!;
            public int StigmaDelta = 0;
        }

        private void DrawFittingString(Graphics g, string text, Font baseFont, Brush brush, RectangleF rect, StringFormat sf)
        {
            // Try to fit text by reducing font size until it fits within rect height, with a minimum font size
            float fontSize = baseFont.Size;
            const float minSize = 12f;
            Font tryFont = baseFont;
            while (fontSize >= minSize)
            {
                tryFont = new Font(baseFont.FontFamily, fontSize, baseFont.Style);
                var sz = g.MeasureString(text, tryFont, new SizeF(rect.Width, rect.Height), sf);
                if (sz.Height <= rect.Height) break;
                fontSize -= 1f;
                tryFont.Dispose();
            }

            // Ensure we don't allocate too many fonts -- use tryFont
            g.DrawString(text, tryFont, brush, rect, sf);
            tryFont.Dispose();
        }

        private class Node
        {
            public string Id = "";
            public string Speaker = "";
            public string Text = "";
            public List<Choice> Choices { get; } = new List<Choice>();
        }

        private readonly Dictionary<string, Node> nodes = new Dictionary<string, Node>();
        private Node? current;
        private int stigma = 0;
        private readonly Action? onFinish;

        public DialogueScreen(Action? onFinish = null)
        {
            this.onFinish = onFinish;
            IsModal = true;

            BuildSampleNodes();
            StartNode("start");
        }

        private void BuildSampleNodes()
        {
            // 五個關於精神健康去污名化的情境題目
            var s1 = new Node { Id = "start", Speaker = "Alex", Text = "我最近常常感到很焦慮，晚上睡不太好，而且工作也難以集中。" };
            // 正確（支持性）=> +1, 不正確（污名化）=> -1
            s1.Choices.Add(new Choice { Text = "你應該硬撐，不要太脆弱", NextId = "q2", StigmaDelta = -1 });
            s1.Choices.Add(new Choice { Text = "抱歉你這麼難受，我在這裡陪你", NextId = "q2", StigmaDelta = 1 });
            s1.Choices.Add(new Choice { Text = "你是不是想太多了，放輕鬆就好", NextId = "q2", StigmaDelta = -1 });
            nodes[s1.Id] = s1;

            var q2 = new Node { Id = "q2", Speaker = "Maya", Text = "我的朋友透露他有憂鬱症，最近常常取消約會，我不知道該怎麼幫他。" };
            q2.Choices.Add(new Choice { Text = "大概只是想引起注意吧", NextId = "q3", StigmaDelta = -1 });
            q2.Choices.Add(new Choice { Text = "試著問他需不需要陪同看醫生或傾聽", NextId = "q3", StigmaDelta = 1 });
            q2.Choices.Add(new Choice { Text = "尊重他的空間，偶爾發訊息關心", NextId = "q3", StigmaDelta = 1 });
            nodes[q2.Id] = q2;

            var q3 = new Node { Id = "q3", Speaker = "Chris", Text = "公司有人開玩笑說『他精神有問題』來形容表現不佳，讓我覺得這樣很傷人。" };
            q3.Choices.Add(new Choice { Text = "這只是玩笑，別太計較", NextId = "q4", StigmaDelta = -1 });
            q3.Choices.Add(new Choice { Text = "你可以跟他說這樣的玩笑會傷害到有需要的人", NextId = "q4", StigmaDelta = 1 });
            q3.Choices.Add(new Choice { Text = "如果你覺得不舒服可以反映給主管", NextId = "q4", StigmaDelta = 1 });
            nodes[q3.Id] = q3;

            var q4 = new Node { Id = "q4", Speaker = "Sam", Text = "有人說去看心理醫師代表很脆弱，我擔心被貼標籤。" };
            q4.Choices.Add(new Choice { Text = "的確會被貼標籤，所以最好不要說", NextId = "q5", StigmaDelta = -1 });
            q4.Choices.Add(new Choice { Text = "尋求幫助是勇氣的表現，並不代表弱點", NextId = "q5", StigmaDelta = 1 });
            q4.Choices.Add(new Choice { Text = "可以私下尋求支持，保護隱私也很重要", NextId = "q5", StigmaDelta = 1 });
            nodes[q4.Id] = q4;

            var q5 = new Node { Id = "q5", Speaker = "Taylor", Text = "我想在社群分享我的經驗，但擔心會被誤解或歧視。" };
            q5.Choices.Add(new Choice { Text = "還是別分享，可能會對職場有影響", NextId = "end", StigmaDelta = -1 });
            q5.Choices.Add(new Choice { Text = "選擇值得信任的人分享，或找支持團體", NextId = "end", StigmaDelta = 1 });
            q5.Choices.Add(new Choice { Text = "可以匿名分享或提供資源給其他人", NextId = "end", StigmaDelta = 1 });
            nodes[q5.Id] = q5;

            var e = new Node { Id = "end", Speaker = "Alex", Text = "謝謝你在這些情境中的回應。支持性的回應能減少污名，鼓勵求助與安全對話。" };
            nodes[e.Id] = e;
        }

        private void StartNode(string id)
        {
            if (!nodes.TryGetValue(id, out var node)) return;
            current = node;
            // 設定按鈕
            RefreshChoices();
        }

        private void RefreshChoices()
        {
            // 移除先前的按鈕
            Elements.RemoveAll(e => e is UIButton);

            if (current == null) return;

            // 對話框下方顯示選項，動態計算按鈕高度以避免超出畫面
            int vw = UIManager.VirtualWidth, vh = UIManager.VirtualHeight;
            float spacing = 12f;
            int count = current.Choices.Count;
            // 固定一致的按鈕寬度/高度（會在可用區域內自動縮放高度但所有按鈕一致）
            float maxBtnW = Math.Min(1100f, DialogW - 160f);
            float btnW = maxBtnW;

            // 與 Draw 使用相同的對話框矩形（使用常數），以避免位置重疊
            var dialogRect = new RectangleF(DialogX, DialogY, DialogW, DialogH);
            float choicesAreaTop = dialogRect.Bottom + spacing;
            float choicesAreaBottom = vh - 40f;
            float areaHeight = Math.Max(0f, choicesAreaBottom - choicesAreaTop);

            // 初始建議高度
            float btnH = 60f;
            float totalNeeded = count * btnH + Math.Max(0, count - 1) * spacing;
            if (totalNeeded > areaHeight && areaHeight > 0f)
            {
                btnH = (areaHeight - Math.Max(0, count - 1) * spacing) / Math.Max(1, count);
                if (btnH < 30f) btnH = 30f; // 最小高度保留點擊區域
            }

            float usedHeight = count * btnH + Math.Max(0, count - 1) * spacing;
            float startY = choicesAreaTop + Math.Max(0f, (areaHeight - usedHeight) / 2f);
            float startX = dialogRect.X + (DialogW - btnW) / 2f;

            for (int i = 0; i < count; i++)
            {
                var ch = current.Choices[i];
                float y = startY + i * (btnH + spacing);
                // 保護: 不要超出底部
                if (y + btnH > choicesAreaBottom) y = Math.Max(choicesAreaTop, choicesAreaBottom - btnH - (count - 1 - i) * (btnH + spacing));
                var btn = new UIButton { Text = ch.Text, Bounds = new RectangleF(startX, y, btnW, btnH) };
                int idx = i; // capture
                btn.Click = () => OnChoiceSelected(current!.Choices[idx]);
                Elements.Add(btn);
            }
        }

        private void OnChoiceSelected(Choice c)
        {
            stigma += c.StigmaDelta;
            if (c.NextId == "end")
            {
                // 顯示結果頁面：清除按鈕並顯示返回
                Elements.RemoveAll(e => e is UIButton);
                // 放在畫面底部中心，避開對話框和選項區域
                // 使用與選項相同寬度與高度風格，並置中於對話框
                float resultW = Math.Min(1100f, DialogW - 160f);
                float resultH = 60f;
                var resultBtn = new UIButton { Text = "返回主選單", Bounds = new RectangleF(DialogX + (DialogW - resultW) / 2f, 620f, resultW, resultH) };
                resultBtn.Click = () => onFinish?.Invoke();
                Elements.Add(resultBtn);

                // 根據支持度產生評價與建議，並顯示在結尾節點文字中
                var endNode = nodes["end"];
                var (grade, advice) = GetEvaluationAndAdvice(stigma);
                endNode.Text = $"謝謝你在這些情境中的回應。支持性的回應能減少污名，鼓勵求助與安全對話。\n\n你的支持度分數: {stigma}\n評價: {grade}\n建議: {advice}";
                current = endNode;
            }
            else
            {
                StartNode(c.NextId);
            }
        }

        private (string grade, string advice) GetEvaluationAndAdvice(int score)
        {
            // score 範圍大致在 -5..+5
            if (score >= 3)
            {
                return ("非常支持", "你提供了很有同理心的回應，繼續以傾聽和接納支持他人。");
            }
            else if (score >= 1)
            {
                return ("支持", "你的回應多半是支持性的，可以再多給予具體協助或資源建議。例：提供陪伴、傾聽或引導尋求專業幫助。");
            }
            else if (score == 0)
            {
                return ("中立", "你的回應有時不明確。試著使用開放式問題與表達關心，以示支持。");
            }
            else if (score >= -2)
            {
                return ("需改進", "你的回應中有些語句可能會讓當事人感到孤立。建議避免責備與簡化情緒，改以傾聽與確認感受。");
            }
            else
            {
                return ("有害", "你的回應可能加深污名與孤立感。請學習用語與支持技巧，並在必要時鼓勵尋求專業協助。");
            }
        }

        public override void Draw(Graphics g)
        {
            // Dialog background box
            using (var bg = new SolidBrush(Color.FromArgb(220, 30, 30, 30)))
            using (var border = new Pen(Color.White))
            {
                var rect = new RectangleF(DialogX, 180f, DialogW, 300f);
                g.FillRectangle(bg, rect);
                g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
            }

            if (current != null)
            {
                using (var nameFont = new Font("微軟正黑體", 18f, FontStyle.Bold))
                using (var textFont = new Font("微軟正黑體", 20f))
                using (var brush = new SolidBrush(Color.White))
                {
                    g.DrawString(current.Speaker, nameFont, brush, 100f, DialogY - 100f);
                    // 如果是結尾節點，給予較大的文字區塊顯示評價與建議
                    RectangleF textRect;
                    if (current.Id == "end")
                    {
                        textRect = new RectangleF(100f, DialogY - 50f, 1040f, DialogH + 500f);
                    }
                    else
                    {
                        textRect = new RectangleF(100f, DialogY - 50f, 1040f, DialogH + 200f);
                    }
                    var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word }
                    ;
                    DrawFittingString(g, current.Text, textFont, brush, textRect, sf);
                }
            }

            // Stigma meter
            DrawStigmaMeter(g);

            base.Draw(g);
        }

        private void DrawStigmaMeter(Graphics g)
        {
            int vw = UIManager.VirtualWidth;
            float x = vw - 280f, y = 40f, w = 220f, h = 28f;
            using (var bg = new SolidBrush(Color.FromArgb(180, 50, 50, 50))) g.FillRectangle(bg, x, y, w, h);
            // stigma range -5..+5 map to 0..1
            float t = (stigma + 5f) / 10f;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            using (var fill = new SolidBrush(Color.FromArgb(220, (int)(200 * (1 - t)), (int)(200 * t), 80)))
            {
                g.FillRectangle(fill, x, y, w * t, h);
            }
            using (var pen = new Pen(Color.White)) g.DrawRectangle(pen, x, y, w, h);
            using (var f = new Font("微軟正黑體", 12f)) g.DrawString($"支持度: {stigma}", f, Brushes.White, x, y - 20f);
        }
    }
}
