(function(){const i=document.createElement("link").relList;if(i&&i.supports&&i.supports("modulepreload"))return;for(const t of document.querySelectorAll('link[rel="modulepreload"]'))n(t);new MutationObserver(t=>{for(const r of t)if(r.type==="childList")for(const o of r.addedNodes)o.tagName==="LINK"&&o.rel==="modulepreload"&&n(o)}).observe(document,{childList:!0,subtree:!0});function s(t){const r={};return t.integrity&&(r.integrity=t.integrity),t.referrerPolicy&&(r.referrerPolicy=t.referrerPolicy),t.crossOrigin==="use-credentials"?r.credentials="include":t.crossOrigin==="anonymous"?r.credentials="omit":r.credentials="same-origin",r}function n(t){if(t.ep)return;t.ep=!0;const r=s(t);fetch(t.href,r)}})();const e={currentPage:"home",balance:5234.5,user:{name:"Пользователь YessGo",email:"user@yessgo.kg"},stories:[{id:1,title:"Бонусы",icon:"sc_bonus.png",pages:["https://picsum.photos/seed/bonus1/1200/2200","https://picsum.photos/seed/bonus2/1200/2200","https://picsum.photos/seed/bonus3/1200/2200"]},{id:2,title:"Йесскоины",icon:"sc_coin.png",pages:["https://picsum.photos/seed/coin1/1200/2200","https://picsum.photos/seed/coin2/1200/2200"]},{id:3,title:"Мы",icon:"sc_we.png",pages:["we_stories.png"]},{id:4,title:"Акции",icon:"sc_sale.png",pages:["sales_stories1.png","sales_stories2.png","sales_stories3.png","sales_stories4.png"]}],banners:[{id:1,image:"banner_1.png"},{id:2,image:"banner_2.png"},{id:3,image:"banner_3.png"}],categories:[{name:"Салоны красоты",image:"cat_beauty.png"},{name:"Аптеки",image:"cat_pharmacy.png"},{name:"Магазины",image:"cat_market.png"}],partnerLogos:["promzona.jpg","faiza.png","navat.png","flask.png","chikenstar.jpg","bublik.jpg","sierra.jpg","ants.jpg","supara.png","teplo.png","savetheales.png"],currentStory:null,currentStoryIndex:0,currentPageIndex:0,storyTimer:null};function g(){_(),E(),"serviceWorker"in navigator&&navigator.serviceWorker.register("/sw.js")}function _(){const a=document.getElementById("app");a.innerHTML=`
        ${w()}
        ${S()}
        ${$()}
        ${P()}
        ${I()}
        ${B()}
        ${k()}
        ${L()}
    `,m(e.currentPage),q()}function w(){return`
        <div class="page" id="home-page">
            <div class="header">
                <div class="profile-section">
                    <div class="profile-avatar">
                        <img src="/images/profile.png" alt="Profile">
                    </div>
                    <div class="profile-info">
                        <div class="profile-name">${e.user.name}</div>
                        <div class="profile-email">${e.user.email}</div>
                    </div>
                </div>

                <div class="wallet-card" onclick="navigateTo('wallet')">
                    <div class="wallet-header">
                        <span class="wallet-label">Ваш Баланс</span>
                        <button class="history-btn">История</button>
                    </div>
                    <div class="wallet-balance">
                        <div class="balance-amount">
                            <span class="balance-value">${e.balance.toFixed(2)}</span>
                            <span class="balance-badge">Yess!Coin</span>
                        </div>
                        <img src="/images/coin.png" alt="Coin" class="coin-icon">
                    </div>
                </div>
            </div>

            <div class="content">
                <h3 class="section-title">Быстрый доступ</h3>
                <div class="stories">
                    ${e.stories.map(a=>`
                        <div class="story-item" onclick="openStory(${a.id})">
                            <div class="story-circle">
                                <img src="/images/${a.icon}" alt="${a.title}">
                            </div>
                            <div class="story-title">${a.title}</div>
                        </div>
                    `).join("")}
                </div>

                <div class="banners">
                    ${e.banners.map(a=>`
                        <div class="banner-item" onclick="openBanner(${a.id})">
                            <img src="/images/${a.image}" alt="Banner">
                        </div>
                    `).join("")}
                </div>

                <div class="categories-scroll">
                    <div class="categories">
                        ${e.categories.map(a=>`
                            <div class="category-item">
                                <div class="category-image">
                                    <img src="/images/${a.image}" alt="${a.name}">
                                </div>
                                <div class="category-title">${a.name}</div>
                            </div>
                        `).join("")}
                        <div class="category-item" onclick="navigateTo('partners')">
                            <div class="category-image" style="background: #F3F3F3; display: flex; align-items: center; justify-content: center;">
                                <span style="font-size: 48px; color: var(--primary-green); font-weight: bold;">+</span>
                            </div>
                            <div class="category-title">Ещё...</div>
                        </div>
                    </div>
                </div>

                <h3 class="section-title">Наши партнёры</h3>
                <div class="partners-section">
                    ${c(0)}
                    ${c(1)}
                    ${c(2)}
                </div>
            </div>
        </div>
    `}function c(a){const i=a===1?[...e.partnerLogos].reverse():e.partnerLogos;return`
        <div class="partners-row">
            ${[...i,...i].map(n=>`
                <div class="partner-logo">
                    <img src="/images/${n}" alt="Partner">
                </div>
            `).join("")}
        </div>
    `}function S(){return`
        <div class="page" id="partners-page">
            <div class="search-bar">
                <input type="text" class="search-input" placeholder="Поиск по компаниям">
                <button class="map-btn">
                    <img src="/images/map_category_icon.png" alt="Map">
                </button>
            </div>

            <div class="categories-grid">
                ${[{name:"Салоны красоты",image:"cat_beauty.png"},{name:"Аптеки",image:"cat_pharmacy.png"},{name:"Магазины",image:"cat_market.png"},{name:"Одежда и обувь",image:"cat_clothes.png"},{name:"Для дома",image:"cat_home.png"},{name:"Электроника",image:"cat_electronics.png"},{name:"Детям",image:"cat_kids.png"},{name:"Спорт",image:"cat_sport.png"},{name:"Еда",image:"cat_food.png"}].map(i=>`
                    <div class="category-card">
                        <img src="/images/${i.image}" alt="${i.name}">
                        <div class="category-card-title">${i.name}</div>
                    </div>
                `).join("")}
            </div>
        </div>
    `}function $(){return`
        <div class="page wallet-page" id="wallet-page">
            <div class="page-header" style="background: transparent;">
                <button class="back-btn" onclick="navigateTo('home')">←</button>
            </div>

            <div class="wallet-scroll">
                <div class="wallet-cards">
                    <div class="wallet-mini-card" style="min-width: 160px;">
                        <div style="font-size: 28px; font-weight: bold; color: var(--primary-green); margin-bottom: 6px;">
                            ${e.balance.toFixed(2)}
                        </div>
                        <div class="balance-badge">Yess!Coin</div>
                    </div>

                    <div class="wallet-mini-card">
                        <img src="/images/coin.png" style="width: 38px; height: 38px; margin-bottom: 6px;">
                        <div style="font-weight: bold; color: var(--primary-green);">Бронза</div>
                        <div style="font-size: 12px; color: var(--text-gray);">Уровни</div>
                    </div>

                    <div class="wallet-mini-card" style="min-width: 180px;">
                        <div style="font-weight: bold; color: var(--primary-green); margin-bottom: 8px;">Что такое йесскоины</div>
                        <button style="background: var(--primary-green); color: white; border: none; border-radius: 12px; padding: 6px 12px; font-weight: bold; cursor: pointer;">Посмотреть</button>
                    </div>

                    <div class="wallet-mini-card">
                        <img src="/images/icon_location.png" style="width: 20px; height: 20px; margin-bottom: 6px;">
                        <div style="font-weight: bold; color: var(--primary-green); font-size: 13px; margin-bottom: 6px;">Адреса наших партнёров</div>
                        <div class="balance-badge" style="font-size: 12px;">1 Адрес</div>
                    </div>
                </div>
            </div>

            <div class="topup-section">
                <div class="topup-title">Выберите сумму пополнения:</div>
                <div class="radio-group">
                    <label class="radio-item"><input type="radio" name="amount" value="1000"> 1000 KGS</label>
                    <label class="radio-item"><input type="radio" name="amount" value="800"> 800 KGS</label>
                    <label class="radio-item"><input type="radio" name="amount" value="600"> 600 KGS</label>
                    <label class="radio-item"><input type="radio" name="amount" value="500"> 500 KGS</label>
                    <label class="radio-item"><input type="radio" name="amount" value="300"> 300 KGS</label>
                    <label class="radio-item"><input type="radio" name="amount" value="100" checked> 100 KGS</label>
                    <label class="radio-item">
                        <input type="radio" name="amount" value="other"> Другая сумма
                        <input type="number" placeholder="Введите сумму" style="margin-left: 8px; padding: 4px 8px; border-radius: 6px; border: 1px solid #ddd;">
                    </label>
                </div>
            </div>

            <button class="topup-btn" onclick="alert('Функция пополнения в демо-версии')">Пополнить</button>
        </div>
    `}function P(){return`
        <div class="page" id="qr-page">
            <div class="qr-demo">
                <h2>QR-страница (демо-версия)</h2>
                <p>Функция сканирования камеры временно отключена.</p>
                <div style="margin-top: 40px;">
                    <img src="/images/nav_qr.png" style="width: 100px; height: 100px; opacity: 0.3;">
                </div>
            </div>
        </div>
    `}function I(){return`
        <div class="page" id="more-page">
            <div class="more-content">
                <div class="profile-card">
                    <div class="profile-avatar">
                        <img src="/images/profile.png" alt="Profile">
                    </div>
                    <div>
                        <div style="font-weight: bold; color: var(--primary-green);">${e.user.name}</div>
                        <div style="font-size: 12px; color: var(--text-gray);">Профиль</div>
                    </div>
                </div>

                <div class="profile-card">
                    <div style="font-weight: bold;">Способы входа</div>
                    <div style="display: flex; gap: 12px; margin-top: 8px;">
                        <img src="/images/icon_phone.png" style="width: 28px; height: 28px;">
                        <img src="/images/icon_google.png" style="width: 28px; height: 28px;">
                    </div>
                </div>

                <div class="menu-card">
                    <img src="/images/icon_location.png" class="menu-icon">
                    <div class="menu-text">Мой город</div>
                </div>

                <div class="menu-card">
                    <img src="/images/icon_promo.png" class="menu-icon">
                    <div class="menu-text">Ввести промокод</div>
                </div>

                <div class="menu-card">
                    <img src="/images/icon_messages.png" class="menu-icon">
                    <div class="menu-text">Сообщения</div>
                </div>

                <div class="menu-card">
                    <img src="/images/icon_referral.png" class="menu-icon">
                    <div class="menu-text">Реферальная ссылка</div>
                </div>

                <div class="menu-card">
                    <img src="/images/icon_history.png" class="menu-icon">
                    <div class="menu-text">История операции</div>
                </div>

                <div class="menu-card">
                    <img src="/images/icon_feedback.png" class="menu-icon">
                    <div class="menu-text">Обратная связь</div>
                </div>

                <div class="menu-card">
                    <img src="/images/icon_certificate.png" class="menu-icon">
                    <div class="menu-text">Сертификаты</div>
                </div>

                <div class="menu-card" onclick="alert('Выход из аккаунта')">
                    <img src="/images/icon_logout.png" class="menu-icon">
                    <div class="menu-text" style="color: var(--primary-green);">Выйти</div>
                </div>
            </div>
        </div>
    `}function B(){return`
        <div class="bottom-nav">
            <div class="bottom-nav-inner">
                <div class="nav-item" data-page="home">
                    <img src="/images/nav_home.png" class="nav-icon" data-normal="/images/nav_home.png" data-active="/images/nav_home_press.png">
                    <div class="nav-text">Главная</div>
                </div>

                <div class="nav-item" data-page="partners">
                    <img src="/images/nav_partners.png" class="nav-icon" data-normal="/images/nav_partners.png" data-active="/images/nav_partners_press.png">
                    <div class="nav-text">Партнёры</div>
                </div>

                <div class="nav-qr" onclick="navigateTo('qr')">
                    <img src="/images/nav_qr.png">
                </div>

                <div class="nav-item" data-page="notifications">
                    <img src="/images/nav_notification.png" class="nav-icon" data-normal="/images/nav_notification.png" data-active="/images/nav_notification_press.png">
                    <div class="nav-text">Уведомления</div>
                </div>

                <div class="nav-item" data-page="more">
                    <img src="/images/nav_more.png" class="nav-icon" data-normal="/images/nav_more.png" data-active="/images/nav_more_press.png">
                    <div class="nav-text">Ещё</div>
                </div>
            </div>
        </div>
    `}function k(){return`
        <div class="story-overlay" id="story-overlay" onclick="closeStory()">
            <div class="story-progress" id="story-progress"></div>
            <div class="story-content">
                <img id="story-image" class="story-image" src="">
                <div class="story-header">
                    <div class="story-avatar">
                        <img id="story-avatar" src="">
                    </div>
                    <div class="story-name" id="story-name"></div>
                </div>
                <div class="story-nav">
                    <div class="story-nav-left" onclick="prevStoryPage(event)"></div>
                    <div class="story-nav-right" onclick="nextStoryPage(event)"></div>
                </div>
            </div>
        </div>
    `}function L(){return`
        <div class="banner-overlay" id="banner-overlay" onclick="closeBanner()">
            <img id="banner-image" src="">
        </div>
    `}function E(){document.querySelectorAll(".nav-item").forEach(a=>{a.addEventListener("click",()=>{const i=a.dataset.page;i&&v(i)})})}function q(){window.openStory=p,window.openBanner=j,window.closeStory=f,window.closeBanner=b,window.nextStoryPage=y,window.prevStoryPage=T,window.navigateTo=v}function v(a){e.currentPage=a,m(a)}function m(a){document.querySelectorAll(".page").forEach(t=>t.classList.remove("active"));const s={home:"home-page",partners:"partners-page",wallet:"wallet-page",qr:"qr-page",notifications:"qr-page",more:"more-page"}[a];s&&document.getElementById(s).classList.add("active"),document.querySelectorAll(".nav-item").forEach(t=>{t.classList.remove("active");const r=t.querySelector(".nav-icon");r&&(r.src=r.dataset.normal)});const n=document.querySelector(`.nav-item[data-page="${a}"]`);if(n){n.classList.add("active");const t=n.querySelector(".nav-icon");t&&(t.src=t.dataset.active)}}function p(a){const i=e.stories.find(l=>l.id===a);if(!i)return;e.currentStory=i,e.currentStoryIndex=0,e.currentPageIndex=0;const s=document.getElementById("story-overlay"),n=document.getElementById("story-progress"),t=document.getElementById("story-image"),r=document.getElementById("story-avatar"),o=document.getElementById("story-name");n.innerHTML=i.pages.map((l,x)=>`
        <div class="progress-bar">
            <div class="progress-fill" id="progress-${x}"></div>
        </div>
    `).join("");const h=i.pages[0].startsWith("http")?i.pages[0]:`/images/${i.pages[0]}`;t.src=h,r.src=`/images/${i.icon}`,o.textContent=i.title,s.classList.add("active"),u()}function u(){e.storyTimer&&clearInterval(e.storyTimer);const a=5500;let i=0;e.storyTimer=setInterval(()=>{i+=50;const s=Math.min(i/a*100,100),n=document.getElementById(`progress-${e.currentPageIndex}`);n&&(n.style.width=`${s}%`),i>=a&&y()},50)}function y(a){if(a&&a.stopPropagation(),!!e.currentStory)if(e.currentPageIndex<e.currentStory.pages.length-1)e.currentPageIndex++,d();else{const i=e.stories.findIndex(s=>s.id===e.currentStory.id)+1;i<e.stories.length?p(e.stories[i].id):f()}}function T(a){if(a&&a.stopPropagation(),!!e.currentStory)if(e.currentPageIndex>0)e.currentPageIndex--,d();else{const i=e.stories.findIndex(s=>s.id===e.currentStory.id)-1;if(i>=0){const s=e.stories[i];e.currentStory=s,e.currentPageIndex=s.pages.length-1,d()}}}function d(){const a=document.getElementById("story-image"),i=e.currentStory.pages[e.currentPageIndex].startsWith("http")?e.currentStory.pages[e.currentPageIndex]:`/images/${e.currentStory.pages[e.currentPageIndex]}`;a.src=i;for(let s=0;s<e.currentStory.pages.length;s++){const n=document.getElementById(`progress-${s}`);n&&(n.style.width=s<e.currentPageIndex?"100%":"0%")}u()}function f(){e.storyTimer&&(clearInterval(e.storyTimer),e.storyTimer=null),document.getElementById("story-overlay").classList.remove("active"),e.currentStory=null}function j(a){const i=e.banners.find(t=>t.id===a);if(!i)return;const s=document.getElementById("banner-overlay"),n=document.getElementById("banner-image");n.src=`/images/${i.image}`,s.classList.add("active"),setTimeout(()=>{b()},25e3)}function b(){document.getElementById("banner-overlay").classList.remove("active")}document.readyState==="loading"?document.addEventListener("DOMContentLoaded",g):g();
