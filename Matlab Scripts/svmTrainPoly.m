function [ confusionMatrix, model, label ] = svmTrainPoly( trX, teX, trY, teY, predictorNames, numlearners,otherArgs)
%svmTrainTest trains and tests an SVM, returns accuracy
%   Detailed explanation goes here
% disp(otherArgs)
% str = sprintf('otherArgs{4} (%d) is scalar: %d and is Greater than 0: %d and is not a float: %d',otherArgs{4}, isscalar(otherArgs{4}), otherArgs{4} > 0, ~isfloat(otherArgs{4}));
% disp(str);
% if(length(otherArgs) > 4) 
%     otherArgs{4} = cast(otherArgs{4}, 'single')
%     disp(otherArgs{4} * 1.10)
% str = sprintf('otherArgs{4} (%d) is scalar: %d and is Greater than 0: %d and is not a float: %d',otherArgs{4}, isscalar(otherArgs{4}), otherArgs{4} > 0, ~isfloat(otherArgs{4}));
% disp(str);
rng(13, 'twister');
sprintf('numlearners = %d', numlearners)
sprintf('length of otherArgs = %d', length(otherArgs))


switch(length(otherArgs))
    case 2
        model = fitcsvm(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2});
    case 4
        model = fitcsvm(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2}, otherArgs{3}, otherArgs{4});
    case 6
        model = fitcsvm(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6});
    case 8
        model = fitcsvm(trX, trY, 'PredictorNames', predictorNames,...
            otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6}, otherArgs{7}, otherArgs{8});
    case 10
        disp('Made it to case 10')
        model = fitcsvm(trX, trY, 'PredictorNames', predictorNames, ...
            otherArgs{1}, otherArgs{2}, otherArgs{3},...
            otherArgs{4}, otherArgs{5}, otherArgs{6}, otherArgs{7}, otherArgs{8}, ...
            otherArgs{9}, otherArgs{10});
    otherwise
        model = fitcsvm(trX, trY, 'PredictorNames', predictorNames );
end

label = predict(model, teX);

confusionMatrix = confusionmat(teY, label);

end
