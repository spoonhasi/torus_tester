# Torus_Tester
---
Torus_Tester는 TORUS를 테스트할 용도로 만들어졌습니다.

## Libraries and Licenses
This project uses the following open-source libraries:

1. **ScottPlot**
   - License: [MIT License](LICENSE/LICENSE_ScottPlot.txt)
   - Copyright: (c) 2018 Scott Harden / Harden Technologies, LLC

2. **Newtonsoft.Json**
   - License: [MIT License](LICENSE/LECENSE_Newtonsoft.Json)
   - Copyright: (c) 2007 James Newton-King

For detailed license information, please refer to the [LICENSE folder](LICENSE/).

## 사용법
---
- 사용하려는 TORUS의 "TORUS/Example_VS19/Api"의 내용물을 본 App의 Api폴더에 복사해야 합니다. 본 App은 TORUS v2.3.0 이상에서 제대로 동작합니다.
- 본 App을 TORUS가 인식하기 위해서 "TorusTester.info" 파일을 사용하려는 TORUS의 "TORUS/Binary/application"에 복사해야 합니다. 
- "MachineStateModelSample.txt"에 사용가능한 MachineStateModel이 저장되어 있습니다.

## Updates
---
v2.3.12
- 오류코드 수정
- 폴더 연결시의 용량추가3종세트를 싱글방식으로 변환

v2.3.11
- 타임아웃 오류코드 수정

v2.3.10
- 시계열 데이터 수집시 현재 Status정보 파악시의 슬립 구간 조정

v2.3.9
- 시계열 데이터 수집 설정 기능 개선

v2.3.8
- 시계열 데이터 수집 설정 기능 메뉴 추가

v2.3.7
- 미구현 기능 버튼 삭제

v2.3.6
- 사용한 라이브러리의 라이센스 파일 추가

v2.3.5
- 모니터링 탭에서 Reset이 표시 안되는 버그 수정

v2.3.4
- 모니터링 탭에 알람 상태 표현 추가

v2.3.3
- MainFile 파일명 표기 관련 임시조치

v2.3.2
- 모니터링 탭의 화면 표시 개선

v2.3.1
- CurrentFile 파일명 표기 관련 임시조치

v2.3.0
- 모니터링 테스트 탭 추가
- Torus 설정 탭 추가
- getData, updateData 항목 검색 기능 추가
- 시계열 데이터 수집 기능 테스트 기능 추가

v2.2.2
- 오류코드 추가 (NC_ERR_NO_TOOL_GROUP, NC_ERR_NO_SELECETD_FILE, NC_ERR_WRONG_NAME_OF_NC_FILE)