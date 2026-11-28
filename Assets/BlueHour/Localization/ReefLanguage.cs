using System;
using System.Collections.Generic;

namespace BlueHour.Localization
{
    public static class ReefLanguage
    {
        private static readonly Dictionary<string, string> Korean = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Manual"] = "자유 유영", ["Fixed"] = "고정 감상", ["Route"] = "경로 유영", ["Follow"] = "생물 따라가기",
            ["Resume"] = "체험 시작", ["Map"] = "지도", ["Settings"] = "설정", ["Field guide"] = "도감",
            ["Journal"] = "일지", ["Album"] = "앨범", ["Close"] = "닫기", ["Back"] = "뒤로",
            ["Exit Blue Hour"] = "Blue Hour 종료", ["Exit Blue Hour?"] = "체험을 종료할까요?",
            ["Exit"] = "종료", ["Cancel"] = "취소", ["Off"] = "끄기", ["5 min"] = "5분",
            ["15 min"] = "15분", ["30 min"] = "30분", ["System"] = "시스템",
            ["Auto"] = "자동", ["Clear"] = "맑음", ["Overcast"] = "흐림", ["Rain"] = "비",
            ["Water"] = "환경음", ["Ambient music"] = "환경 음악", ["Alternate"] = "대체 음악",
            ["Mute"] = "음소거", ["Low"] = "낮음", ["High"] = "높음",
            ["Restart timer"] = "타이머 다시 시작", ["Viewing speed"] = "감상 속도", ["Timer"] = "타이머",
            ["Depth and habitat HUD"] = "수심·서식지 표시", ["Species focus HUD"] = "생물 정보 표시",
            ["Environment panel"] = "환경 정보 표시", ["Hide all HUD"] = "정보 표시 모두 숨기기",
            ["Water effects"] = "수중 효과", ["Frame limit"] = "FPS 제한", ["Ambience"] = "환경음", ["Music"] = "음악",
            ["north up"] = "위쪽이 북쪽", ["Not observed yet"] = "관찰 기록 없음",
            ["Show evidence and behavior"] = "행동과 출처 보기", ["Behavior"] = "행동",
            ["Response"] = "접근 반응", ["No first observation"] = "최초 관찰 기록 없음",
            ["Confirm"] = "확인", ["Delete"] = "삭제", ["Note deleted."] = "메모를 삭제했습니다.",
            ["New note"] = "새 메모", ["Edit note"] = "메모 편집", ["Attach a photo"] = "사진 연결",
            ["Change or remove photo"] = "연결 사진 변경 또는 해제", ["Photo unavailable"] = "사진을 불러올 수 없습니다.",
            ["Linked photo"] = "연결한 사진", ["Save note"] = "메모 저장", ["Note saved."] = "메모를 저장했습니다.",
            ["Clear note"] = "메모 비우기", ["Favorites only"] = "즐겨찾기만", ["Filter by species"] = "생물별 보기",
            ["Unfavorite"] = "즐겨찾기 해제", ["Favorite"] = "즐겨찾기 추가", ["Export PNG"] = "PNG 내보내기",
            ["Confirm deletion"] = "삭제 확인", ["Delete photo"] = "사진 삭제", ["Photo removed."] = "앨범에서 사진을 삭제했습니다.",
            ["Photo saved."] = "사진을 저장했습니다.", ["First seen"] = "최초 관찰",
            ["Photo · wheel to zoom · Enter to capture · Esc"] = "사진 · 휠 줌 · Enter 촬영 · Esc",
            ["B: field guide"] = "B: 도감", ["depth"] = "수심", ["Outside region"] = "해역 밖",
            ["Queensland"] = "퀸즐랜드", ["LocalTime"] = "현지 시각", ["Summer"] = "여름",
            ["Autumn"] = "가을", ["Winter"] = "겨울", ["Spring"] = "봄",
            ["manual"] = "수동", ["local"] = "현지", ["simulated"] = "시뮬레이션",
            ["Salinity"] = "염분", ["absolute"] = "절대압", ["Light"] = "광량", ["Current"] = "해류",
            ["F1 Close  ·  F2 Clock  ·  F3 Fixed hour"] = "F1 닫기 · F2 시각 · F3 고정 시각",
            ["F4 Season  ·  F5 Weather"] = "F4 계절 · F5 날씨"
        };

        private static readonly Dictionary<string, string> Education = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["amphiprion_melanopus"] = "시나몬 흰동가리는 숙주 말미잘 주변을 유영하고, 위협을 느끼면 그 곁으로 돌아갑니다.",
            ["entacmaea_quadricolor"] = "버블팁 말미잘은 산호초 바닥에 자리 잡고 시나몬 흰동가리의 숙주가 됩니다.",
            ["chromis_viridis"] = "그린 크로미스는 산호 가까이에서 무리를 이루어 유영합니다.",
            ["taeniura_lymma"] = "파란점가오리는 암초와 모래 해저 가까이에서 지냅니다.",
            ["acropora_millepora"] = "밀레포라 가지산호는 그레이트 배리어 리프에서 관찰된 가지 모양 돌산호입니다."
        };

        public static bool IsKorean { get; set; } = true;

        public static string Describe(string speciesId, string english) =>
            IsKorean && Education.TryGetValue(speciesId, out var translation) ? translation : english;

        public static string Text(string english)
        {
            return IsKorean && Korean.TryGetValue(english, out var translation) ? translation : english;
        }

        public static bool HasTranslation(string english) =>
            Korean.TryGetValue(english, out var translation) && !string.IsNullOrWhiteSpace(translation);
    }
}
